using BSX.BuildingBlocks.Persistence;
using BSX.SharedKernel.Primitives;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

internal sealed class FakeModuleUnitOfWork : IModuleUnitOfWork
{
    private readonly List<IDomainEvent> _events;

    public FakeModuleUnitOfWork(string moduleName = "Test", params IDomainEvent[] events)
    {
        ModuleName = moduleName;
        _events = [.. events];
    }

    public string ModuleName { get; }

    public int SaveCount { get; private set; }

    public bool Cleared { get; private set; }

    public FakeDatabaseTransaction? LastTransaction { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        LastTransaction = new FakeDatabaseTransaction();
        return Task.FromResult<IDatabaseTransaction>(LastTransaction);
    }

    public Task<int> SaveChangesAndDispatchAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(cancellationToken);

    public IReadOnlyCollection<IDomainEvent> CollectPendingDomainEvents() => _events.ToArray();

    public void ClearPendingDomainEvents()
    {
        _events.Clear();
        Cleared = true;
    }
}
