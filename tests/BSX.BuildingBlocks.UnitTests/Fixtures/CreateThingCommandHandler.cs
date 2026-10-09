using BSX.BuildingBlocks.Cqrs;
using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed class CreateThingCommandHandler : ICommandHandler<CreateThingCommand, Guid>
{
    public Task<Result<Guid>> HandleAsync(CreateThingCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Success(Guid.NewGuid()));
}
