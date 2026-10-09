using System.Collections.Concurrent;
using BSX.BuildingBlocks.Events;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed class TestDomainEventHandler : IDomainEventHandler<TestDomainEvent>
{
    public static ConcurrentBag<Guid> Handled { get; } = [];

    public static void Reset() => Handled.Clear();

    public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Handled.Add(domainEvent.EventId);
        return Task.CompletedTask;
    }
}
