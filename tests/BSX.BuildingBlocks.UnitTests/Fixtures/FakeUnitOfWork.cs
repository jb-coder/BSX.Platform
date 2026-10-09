using BSX.BuildingBlocks.Persistence;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public int SaveAndDispatchCount { get; private set; }

    public FakeDatabaseTransaction? LastTransaction { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(0);
    }

    public Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        LastTransaction = new FakeDatabaseTransaction();
        return Task.FromResult<IDatabaseTransaction>(LastTransaction);
    }

    public async Task<int> SaveChangesAndDispatchAsync(CancellationToken cancellationToken = default)
    {
        SaveAndDispatchCount++;

        if (LastTransaction is not null)
        {
            await LastTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }
}
