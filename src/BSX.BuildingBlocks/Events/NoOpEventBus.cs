using Microsoft.Extensions.Logging;

namespace BSX.BuildingBlocks.Events;

/// <summary>
/// Default <see cref="IEventBus"/> used before a broker is configured. Integration rows remain in
/// the outbox until an integration publisher replaces this implementation (ADR-009), so resolving
/// the bus never fails and no event is silently dropped.
/// </summary>
internal sealed class NoOpEventBus : IEventBus
{
    private readonly ILogger<NoOpEventBus> _logger;

    /// <summary>Initializes a new instance of the <see cref="NoOpEventBus"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public NoOpEventBus(ILogger<NoOpEventBus> logger) => _logger = logger;

    /// <inheritdoc />
    public Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        _logger.LogDebug(
            "No event bus is configured; integration event {EventType} ({EventId}) was not published.",
            typeof(TIntegrationEvent).Name,
            integrationEvent.EventId);

        return Task.CompletedTask;
    }
}
