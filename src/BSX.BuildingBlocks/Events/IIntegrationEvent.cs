namespace BSX.BuildingBlocks.Events;

/// <summary>
/// Represents an event published between modules.
/// Integration events are the only permitted direct communication channel between modules
/// and are designed to travel through the Outbox Pattern and an external message broker.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Gets the unique identifier of the integration event occurrence.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the date and time, in UTC, when the integration event occurred.
    /// </summary>
    DateTimeOffset OccurredOnUtc { get; }
}
