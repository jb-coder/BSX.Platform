using BSX.SharedKernel.Primitives;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed record TestDomainEvent(Guid Payload) : DomainEvent;
