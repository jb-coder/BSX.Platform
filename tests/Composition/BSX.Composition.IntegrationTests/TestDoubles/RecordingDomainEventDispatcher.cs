using BSX.BuildingBlocks.Events;
using BSX.SharedKernel.Primitives;

namespace BSX.Composition.IntegrationTests.TestDoubles;

internal sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
{
    public List<IDomainEvent> Dispatched { get; } = [];

    public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        Dispatched.AddRange(domainEvents);
        return Task.CompletedTask;
    }
}
