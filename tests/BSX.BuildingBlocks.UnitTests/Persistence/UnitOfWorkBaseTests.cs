using BSX.BuildingBlocks.Events;
using BSX.BuildingBlocks.Persistence;
using BSX.BuildingBlocks.UnitTests.Fixtures;
using BSX.SharedKernel.Primitives;

namespace BSX.BuildingBlocks.UnitTests.Persistence;

public sealed class UnitOfWorkBaseTests
{
    [Fact]
    public async Task Should_CollectPersistCommitClearAndDispatch()
    {
        var dispatcher = new RecordingDomainEventDispatcher();
        var unitOfWork = new RecordingUnitOfWork(dispatcher);
        var domainEvent = new TestDomainEvent(Guid.NewGuid());
        unitOfWork.EventsToCollect = [domainEvent];

        int written = await unitOfWork.SaveChangesAndDispatchAsync();

        written.Should().Be(1);
        unitOfWork.Collected.Should().BeTrue();
        unitOfWork.Saved.Should().BeTrue();
        unitOfWork.Committed.Should().BeTrue();
        unitOfWork.Cleared.Should().BeTrue();
        dispatcher.Dispatched.Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
    }

    [Fact]
    public async Task Should_NotDispatch_WhenNoEventsPending()
    {
        var dispatcher = new RecordingDomainEventDispatcher();
        var unitOfWork = new RecordingUnitOfWork(dispatcher);

        await unitOfWork.SaveChangesAndDispatchAsync();

        dispatcher.Dispatched.Should().BeEmpty();
        unitOfWork.Committed.Should().BeTrue();
        unitOfWork.Cleared.Should().BeTrue();
    }

    private sealed class RecordingUnitOfWork : UnitOfWorkBase
    {
        public RecordingUnitOfWork(IDomainEventDispatcher domainEventDispatcher)
            : base(domainEventDispatcher)
        {
        }

        public IReadOnlyCollection<IDomainEvent> EventsToCollect { get; set; } = [];

        public bool Collected { get; private set; }

        public bool Saved { get; private set; }

        public bool Committed { get; private set; }

        public bool Cleared { get; private set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saved = true;
            return Task.FromResult(1);
        }

        public override Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        protected override IReadOnlyCollection<IDomainEvent> CollectDomainEvents()
        {
            Collected = true;
            return EventsToCollect;
        }

        protected override void ClearDomainEvents() => Cleared = true;

        protected override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Committed = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
    {
        public List<IDomainEvent> Dispatched { get; } = [];

        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            Dispatched.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }
}
