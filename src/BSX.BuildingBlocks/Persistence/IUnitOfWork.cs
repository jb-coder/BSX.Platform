namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Represents a transactional boundary over the persistence store.
/// Commands that modify business data must run inside a transaction; queries must not.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The number of state entries written to the store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The started transaction.</returns>
    Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes, commits the transaction opened for the command, and then
    /// dispatches the domain events raised by the affected aggregates (ADR-009).
    /// This is the single commit and dispatch seam used by the command pipeline.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The number of state entries written to the store.</returns>
    Task<int> SaveChangesAndDispatchAsync(CancellationToken cancellationToken = default);
}
