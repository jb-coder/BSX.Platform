using BSX.BuildingBlocks.Cqrs;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed record CreateThingCommand(string Name) : ICommand<Guid>;
