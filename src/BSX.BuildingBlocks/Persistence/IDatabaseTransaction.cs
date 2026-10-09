namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Represents an explicit database transaction.
/// </summary>
public interface IDatabaseTransaction : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls the transaction back.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
