using BSX.BuildingBlocks.DependencyInjection;
using BSX.BuildingBlocks.Events;
using BSX.BuildingBlocks.UnitTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.BuildingBlocks.UnitTests.Events;

public sealed class DomainEventDispatcherTests
{
    [Fact]
    public async Task Should_DispatchEvent_ToRegisteredHandlers()
    {
        TestDomainEventHandler.Reset();
        IDomainEventDispatcher dispatcher = CreateDispatcher();
        var domainEvent = new TestDomainEvent(Guid.NewGuid());

        await dispatcher.DispatchAsync([domainEvent]);

        TestDomainEventHandler.Handled.Should().ContainSingle().Which.Should().Be(domainEvent.EventId);
    }

    [Fact]
    public async Task Should_DoNothing_WhenNoEventsAreSupplied()
    {
        TestDomainEventHandler.Reset();
        IDomainEventDispatcher dispatcher = CreateDispatcher();

        await dispatcher.DispatchAsync([]);

        TestDomainEventHandler.Handled.Should().BeEmpty();
    }

    private static IDomainEventDispatcher CreateDispatcher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBuildingBlocks(typeof(DomainEventDispatcherTests).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IDomainEventDispatcher>();
    }
}
