using BSX.BuildingBlocks.Persistence;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>Records <see cref="IUnitOfWork.SaveChangesAsync"/> calls without persisting.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
