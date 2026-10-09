using BSX.BuildingBlocks.Cqrs;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed record PingQuery(int Value) : IQuery<int>;
