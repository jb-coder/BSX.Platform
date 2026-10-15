using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

[AllowAnonymous]
public sealed record FailingCommand : ICommand;
