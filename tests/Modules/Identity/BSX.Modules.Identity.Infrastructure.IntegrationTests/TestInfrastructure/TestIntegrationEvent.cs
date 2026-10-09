using BSX.BuildingBlocks.Events;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;

internal sealed record TestIntegrationEvent(Guid EventId, DateTimeOffset OccurredOnUtc) : IIntegrationEvent;
