using BSX.SharedKernel.Primitives;

namespace BSX.SharedKernel.UnitTests.Primitives;

public sealed class AggregateRootTests
{
    [Fact]
    public void Should_RaiseDomainEvent_WhenBusinessBehaviorRuns()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DoWork();

        aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TestDomainEvent>();
    }

    [Fact]
    public void Should_ClearDomainEvents_WhenRequested()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.DoWork();

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Throw_WhenDomainEventIsNull()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        Action act = () => aggregate.Raise(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class TestAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        public void DoWork() => RaiseDomainEvent(new TestDomainEvent());

        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestDomainEvent : DomainEvent;
}
