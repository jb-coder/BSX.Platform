using System.Diagnostics;
using System.Reflection;
using BSX.BuildingBlocks.Diagnostics;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BSX.BuildingBlocks.Events;

/// <summary>
/// In-process implementation of <see cref="IDomainEventDispatcher"/>.
/// Resolves every <see cref="IDomainEventHandler{TDomainEvent}"/> registered for the runtime
/// type of each domain event.
/// </summary>
/// <remarks>
/// When one or more <see cref="IOutboxStore"/> instances are registered, only events that were
/// atomically claimed from the outbox are dispatched. This gives effectively-once dispatch per
/// attempt across concurrent requests and background workers (ADR-009). Handler failures are
/// isolated, recorded against the outbox row, and retried or dead-lettered by the recovery loop.
/// Without a registered store (pure unit scenarios) events are dispatched directly for
/// backward compatibility.
/// </remarks>
internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private const string HandleMethodName = "HandleAsync";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetService<ILogger<DomainEventDispatcher>>()
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DomainEventDispatcher>.Instance;
    }

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        IDomainEvent[] events = domainEvents.ToArray();

        if (events.Length == 0)
        {
            return;
        }

        using Activity? activity = PlatformDiagnostics.ActivitySource.StartActivity("outbox.dispatch_domain_events");
        activity?.SetTag("bsx.domain_event.count", events.Length);

        IOutboxStore[] stores = _serviceProvider.GetServices<IOutboxStore>().ToArray();
        Dictionary<Guid, (IDomainEvent Event, OutboxEnvelope Envelope, IOutboxStore Store)> dispatchable = [];

        if (stores.Length == 0)
        {
            foreach (IDomainEvent domainEvent in events)
            {
                dispatchable[domainEvent.EventId] = (domainEvent, null!, null!);
            }
        }
        else
        {
            Guid[] eventIds = events.Select(domainEvent => domainEvent.EventId).Distinct().ToArray();

            foreach (IOutboxStore store in stores)
            {
                IReadOnlyList<OutboxEnvelope> claimed = await store
                    .ClaimAsync(eventIds, cancellationToken)
                    .ConfigureAwait(false);

                foreach (OutboxEnvelope envelope in claimed)
                {
                    IDomainEvent? domainEvent = events.FirstOrDefault(candidate => candidate.EventId == envelope.EventId);

                    if (domainEvent is not null)
                    {
                        dispatchable[envelope.EventId] = (domainEvent, envelope, store);
                    }
                }
            }

            if (dispatchable.Count == 0)
            {
                _logger.LogDebug(
                    "No outbox rows could be claimed for {EventCount} domain event(s); another worker owns them or the events were not persisted.",
                    events.Length);
                return;
            }
        }

        foreach ((IDomainEvent domainEvent, OutboxEnvelope envelope, IOutboxStore store) in dispatchable.Values)
        {
            string handlerName = "none";
            int failures = 0;
            int handlerCount = 0;

            Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            MethodInfo handleMethod = handlerType.GetMethod(HandleMethodName)!;

            foreach (object? handler in _serviceProvider.GetServices(handlerType))
            {
                if (handler is null)
                {
                    continue;
                }

                handlerCount++;
                handlerName = handler.GetType().Name;
                long startedAt = Stopwatch.GetTimestamp();

                try
                {
                    var task = (Task?)handleMethod.Invoke(handler, [domainEvent, cancellationToken]);
                    if (task is not null)
                    {
                        await task.ConfigureAwait(false);
                    }

                    PlatformDiagnostics.DomainEventHandlersExecuted.Add(1);
                }
                catch (Exception exception)
                {
                    failures++;
                    PlatformDiagnostics.DomainEventHandlersFailed.Add(1);
                    _logger.LogError(
                        exception,
                        "Domain event handler {Handler} failed for {EventType} ({EventId}).",
                        handlerName,
                        domainEvent.GetType().Name,
                        domainEvent.EventId);
                }
                finally
                {
                    PlatformDiagnostics.DomainEventHandlerDuration.Record(
                        Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                        new KeyValuePair<string, object?>("event_type", domainEvent.GetType().Name),
                        new KeyValuePair<string, object?>("handler", handlerName));
                }
            }

            if (envelope is null)
            {
                // Backward-compatible path for callers without an outbox (unit tests, tooling).
                if (failures > 0)
                {
                    PlatformDiagnostics.DomainEventsFailed.Add(1);
                }

                continue;
            }

            if (failures == 0)
            {
                await store.MarkProcessedAsync([domainEvent.EventId], cancellationToken).ConfigureAwait(false);
                PlatformDiagnostics.DomainEventsDispatched.Add(1);
                PlatformDiagnostics.OutboxMessagesProcessed.Add(
                    1,
                    new KeyValuePair<string, object?>("module", store.ModuleName));
            }
            else
            {
                await store.MarkFailedAsync(
                    [domainEvent.EventId],
                    $"Domain event handler failure(s): {failures}.",
                    cancellationToken).ConfigureAwait(false);
                PlatformDiagnostics.DomainEventsFailed.Add(1);
            }

            _logger.LogDebug(
                "Dispatched domain event {EventType} ({EventId}) from {Module} to {HandlerCount} handler(s) with {FailureCount} failure(s).",
                domainEvent.GetType().Name,
                domainEvent.EventId,
                store.ModuleName,
                handlerCount,
                failures);
        }
    }
}
