namespace BSX.SharedKernel.Primitives;

/// <summary>
/// The base class for aggregate roots.
/// An aggregate root is the single entry point to a consistency boundary and the only
/// place where domain events may be raised.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot{TId}"/> class.
    /// Required by object-relational mappers.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Gets the domain events raised by this aggregate but not yet dispatched.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Records a domain event to be dispatched after the aggregate state is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to record.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all recorded domain events.
    /// Called by the persistence layer after the events have been dispatched.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
