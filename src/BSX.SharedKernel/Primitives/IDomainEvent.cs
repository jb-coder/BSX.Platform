namespace BSX.SharedKernel.Primitives;

/// <summary>
/// Represents a domain event raised by an aggregate root.
/// Domain events describe something meaningful that already happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the domain event occurrence.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the date and time, in UTC, when the domain event occurred.
    /// </summary>
    DateTimeOffset OccurredOnUtc { get; }
}
