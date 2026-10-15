using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

[AllowAnonymous]
public sealed record PingQuery(int Value) : IQuery<int>;
