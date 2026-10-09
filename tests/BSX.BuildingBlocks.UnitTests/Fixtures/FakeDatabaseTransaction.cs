using BSX.BuildingBlocks.Persistence;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

internal sealed class FakeDatabaseTransaction : IDatabaseTransaction
{
    public bool Committed { get; private set; }

    public bool RolledBack { get; private set; }

    public bool Disposed { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Committed = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RolledBack = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
