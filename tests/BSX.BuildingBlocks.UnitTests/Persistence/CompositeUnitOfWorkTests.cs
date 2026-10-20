using BSX.BuildingBlocks.Persistence;
using BSX.BuildingBlocks.UnitTests.Fixtures;

namespace BSX.BuildingBlocks.UnitTests.Persistence;

public sealed class CompositeUnitOfWorkTests
{
    [Fact]
    public void Should_Throw_WhenNoModuleIsRegistered()
    {
        Action act = () => new CompositeUnitOfWork([], new RecordingDomainEventDispatcher());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_SaveAllModules()
    {
        var first = new FakeModuleUnitOfWork("Identity");
        var second = new FakeModuleUnitOfWork("Crm");
        var composite = new CompositeUnitOfWork([first, second], new RecordingDomainEventDispatcher());

        int written = await composite.SaveChangesAsync();

        written.Should().Be(2);
        first.SaveCount.Should().Be(1);
        second.SaveCount.Should().Be(1);
        composite.ModuleNames.Should().BeEquivalentTo(["Identity", "Crm"]);
    }

    [Fact]
    public async Task Should_CommitAllModuleTransactions_AndDispatchEachEventOnce()
    {
        var dispatcher = new RecordingDomainEventDispatcher();
        var firstEvent = new TestDomainEvent(Guid.NewGuid());
        var secondEvent = new TestDomainEvent(Guid.NewGuid());
        var first = new FakeModuleUnitOfWork("Identity", firstEvent);
        var second = new FakeModuleUnitOfWork("Crm", secondEvent);
        var composite = new CompositeUnitOfWork([first, second], dispatcher);

        await using IDatabaseTransaction transaction = await composite.BeginTransactionAsync();
        await composite.SaveChangesAndDispatchAsync();

        first.LastTransaction!.Committed.Should().BeTrue();
        second.LastTransaction!.Committed.Should().BeTrue();
        dispatcher.Dispatched.Should().BeEquivalentTo([firstEvent, secondEvent]);
        first.Cleared.Should().BeTrue();
        second.Cleared.Should().BeTrue();
    }

    [Fact]
    public async Task Should_NotDispatchAgain_WhenSaveAndDispatchIsRepeated()
    {
        var dispatcher = new RecordingDomainEventDispatcher();
        var domainEvent = new TestDomainEvent(Guid.NewGuid());
        var module = new FakeModuleUnitOfWork("Identity", domainEvent);
        var composite = new CompositeUnitOfWork([module], dispatcher);

        await composite.SaveChangesAndDispatchAsync();
        await composite.SaveChangesAndDispatchAsync();

        dispatcher.Dispatched.Should().ContainSingle();
    }

    [Fact]
    public async Task Should_RollbackAllModuleTransactions()
    {
        var first = new FakeModuleUnitOfWork("Identity");
        var second = new FakeModuleUnitOfWork("Crm");
        var composite = new CompositeUnitOfWork([first, second], new RecordingDomainEventDispatcher());

        await using IDatabaseTransaction transaction = await composite.BeginTransactionAsync();
        await transaction.RollbackAsync();

        first.LastTransaction!.RolledBack.Should().BeTrue();
        second.LastTransaction!.RolledBack.Should().BeTrue();
    }
}
