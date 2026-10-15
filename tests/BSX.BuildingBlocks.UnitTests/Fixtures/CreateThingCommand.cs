using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

[AllowAnonymous]
public sealed record CreateThingCommand(string Name) : ICommand<Guid>;
