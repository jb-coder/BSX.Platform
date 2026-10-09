using BSX.BuildingBlocks.Cqrs;
using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed class FailingCommandHandler : ICommandHandler<FailingCommand>
{
    public Task<Result> HandleAsync(FailingCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure(Error.Conflict("Thing.Conflict", "The thing conflicts with the current state.")));
}
