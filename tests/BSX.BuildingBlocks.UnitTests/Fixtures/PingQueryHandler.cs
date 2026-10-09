using BSX.BuildingBlocks.Cqrs;
using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed class PingQueryHandler : IQueryHandler<PingQuery, int>
{
    public Task<Result<int>> HandleAsync(PingQuery query, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(query.Value * 2));
}
