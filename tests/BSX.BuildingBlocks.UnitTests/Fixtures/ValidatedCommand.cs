using BSX.BuildingBlocks.Cqrs;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed record ValidatedCommand(string Name) : ICommand;
