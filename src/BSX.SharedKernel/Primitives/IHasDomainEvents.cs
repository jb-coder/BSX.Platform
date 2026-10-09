namespace BSX.SharedKernel.Primitives;

/// <summary>
/// Exposes the domain events raised by an aggregate so that the persistence layer can collect and
/// dispatch them without referencing the generic aggregate type (ADR-009).
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Gets the domain events raised by the aggregate but not yet dispatched.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Clears all recorded domain events.</summary>
    void ClearDomainEvents();
}
