using BSX.BuildingBlocks.Events;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Outbox;

/// <summary>Enqueues integration events into the transactional outbox (ADR-009).</summary>
public interface IOutboxWriter
{
    /// <summary>Enqueues an integration event to be persisted in the current transaction.</summary>
    /// <param name="integrationEvent">The integration event.</param>
    void Enqueue(IIntegrationEvent integrationEvent);
}
