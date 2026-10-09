using BSX.BuildingBlocks.Events;
using BSX.SharedKernel.Primitives;

namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Base implementation of the persistence seams defined by ADR-009.
/// Infrastructure derives from this type and provides the store-specific hooks; the base owns the
/// ordered post-commit flow: collect domain events, persist state and events, commit, clear, and
/// only then dispatch.
/// </summary>
public abstract class UnitOfWorkBase : IUnitOfWork
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>Initializes a new instance of the <see cref="UnitOfWorkBase"/> class.</summary>
    /// <param name="domainEventDispatcher">The domain event dispatcher used after commit.</param>
    protected UnitOfWorkBase(IDomainEventDispatcher domainEventDispatcher)
        => _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));

    /// <inheritdoc />
    public abstract Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public async Task<int> SaveChangesAndDispatchAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<IDomainEvent> domainEvents = CollectDomainEvents();

        int written = await SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await CommitAsync(cancellationToken).ConfigureAwait(false);

        ClearDomainEvents();

        if (domainEvents.Count > 0)
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        }

        return written;
    }

    /// <summary>
    /// Collects the pending domain events from the tracked aggregates before they are persisted.
    /// </summary>
    /// <returns>The pending domain events.</returns>
    protected abstract IReadOnlyCollection<IDomainEvent> CollectDomainEvents();

    /// <summary>
    /// Clears the domain events of the tracked aggregates once they have been persisted.
    /// </summary>
    protected abstract void ClearDomainEvents();

    /// <summary>
    /// Commits the transaction opened for the current command.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    protected abstract Task CommitAsync(CancellationToken cancellationToken = default);
}
