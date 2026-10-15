using BSX.BuildingBlocks.Cqrs;
using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

public sealed class RequirePermissionCommandHandler : ICommandHandler<RequirePermissionCommand>
{
    public static int InvocationCount { get; private set; }

    public static void Reset() => InvocationCount = 0;

    public Task<Result> HandleAsync(RequirePermissionCommand request, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        return Task.FromResult(Result.Success());
    }
}
