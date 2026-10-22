using System.Diagnostics;
using BSX.BuildingBlocks.Diagnostics;
using BSX.BuildingBlocks.Events;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BSX.Persistence.Outbox;

/// <summary>
/// Background crash-recovery loop for the transactional outbox (ADR-009). It requeues stale
/// claims, then dispatches pending domain rows and publishes integration rows. Claiming is atomic,
/// so multiple instances can run this loop safely.
/// </summary>
public sealed class OutboxRecoveryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxRecoveryService> _logger;

    /// <summary>Initializes a new instance of the <see cref="OutboxRecoveryService"/> class.</summary>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="options">The outbox options.</param>
    /// <param name="logger">The logger.</param>
    public OutboxRecoveryService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxRecoveryService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.ProcessingEnabled)
        {
            _logger.LogInformation("Outbox recovery is disabled.");
            return;
        }

        bool firstPass = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAsync(firstPass, stoppingToken).ConfigureAwait(false);
                firstPass = false;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox recovery pass failed; retrying after the poll interval.");
            }

            try
            {
                await Task.Delay(_options.PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessAsync(bool requeueStale, CancellationToken cancellationToken)
    {
        using Activity? activity = PlatformDiagnostics.ActivitySource.StartActivity("outbox.recovery_pass");
        using IServiceScope scope = _scopeFactory.CreateScope();

        IOutboxStore[] stores = scope.ServiceProvider.GetServices<IOutboxStore>().ToArray();
        IDomainEventDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        int backlog = 0;

        foreach (IOutboxStore store in stores)
        {
            if (requeueStale)
            {
                await store.RequeueStaleAsync(_options.StaleProcessingAfter, cancellationToken).ConfigureAwait(false);
            }

            IReadOnlyList<OutboxEnvelope> envelopes = await store
                .ReadPendingAsync(_options.BatchSize, cancellationToken)
                .ConfigureAwait(false);

            var domainEvents = new List<IDomainEvent>(envelopes.Count);

            foreach (OutboxEnvelope envelope in envelopes)
            {
                if (envelope.Kind == OutboxKinds.Integration)
                {
                    await PublishIntegrationAsync(scope, store, envelope, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                IDomainEvent? domainEvent = DomainEventSerializer.Deserialize(envelope);

                if (domainEvent is null)
                {
                    await store.MarkFailedAsync(
                        [envelope.EventId],
                        $"Could not deserialize domain event '{envelope.EventType}'.",
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogError(
                        "Could not deserialize outbox domain event {EventType} ({EventId}) from {Module}.",
                        envelope.EventType,
                        envelope.EventId,
                        store.ModuleName);
                    continue;
                }

                domainEvents.Add(domainEvent);
            }

            if (domainEvents.Count > 0)
            {
                await dispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
            }

            backlog += await store.CountPendingAsync(cancellationToken).ConfigureAwait(false);
        }

        PlatformDiagnostics.SetOutboxBacklog(backlog);
        activity?.SetTag("bsx.outbox.store_count", stores.Length);
        activity?.SetTag("bsx.outbox.backlog", backlog);
    }

    private async Task PublishIntegrationAsync(
        IServiceScope scope,
        IOutboxStore store,
        OutboxEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!_options.IntegrationPublishingEnabled)
        {
            return;
        }

        IEventBus? eventBus = scope.ServiceProvider.GetService<IEventBus>();

        if (eventBus is null)
        {
            _logger.LogWarning("Integration publishing is enabled but no IEventBus is registered.");
            return;
        }

        try
        {
            await eventBus.PublishAsync(new RawIntegrationEvent(envelope), cancellationToken).ConfigureAwait(false);
            await store.MarkProcessedAsync([envelope.EventId], cancellationToken).ConfigureAwait(false);
            PlatformDiagnostics.OutboxMessagesProcessed.Add(
                1,
                new KeyValuePair<string, object?>("module", store.ModuleName),
                new KeyValuePair<string, object?>("kind", OutboxKinds.Integration));
        }
        catch (Exception exception)
        {
            await store.MarkFailedAsync([envelope.EventId], exception.Message, cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                exception,
                "Failed to publish integration event {EventType} ({EventId}) from {Module}.",
                envelope.EventType,
                envelope.EventId,
                store.ModuleName);
        }
    }

    /// <summary>An integration envelope handed to the configured event bus.</summary>
    private sealed record RawIntegrationEvent(OutboxEnvelope Envelope) : IIntegrationEvent
    {
        public Guid EventId => Envelope.EventId;

        public DateTimeOffset OccurredOnUtc => Envelope.OccurredOnUtc;
    }
}
