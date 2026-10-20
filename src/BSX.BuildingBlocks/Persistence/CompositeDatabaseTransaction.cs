namespace BSX.BuildingBlocks.Persistence;

/// <summary>
/// Aggregates the transactions opened by the composed module units of work. Module transactions are
/// committed in order and rolled back in reverse order. Cross-module atomicity across separate
/// connections is not guaranteed; a command should target a single module aggregate.
/// </summary>
internal sealed class CompositeDatabaseTransaction : IDatabaseTransaction
{
    private readonly IReadOnlyList<IDatabaseTransaction> _transactions;

    public CompositeDatabaseTransaction(IReadOnlyList<IDatabaseTransaction> transactions)
        => _transactions = transactions;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        foreach (IDatabaseTransaction transaction in _transactions)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        for (int index = _transactions.Count - 1; index >= 0; index--)
        {
            await _transactions[index].RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (IDatabaseTransaction transaction in _transactions)
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}
