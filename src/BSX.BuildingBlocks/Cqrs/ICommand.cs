using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// Marker abstraction for a command that modifies state and does not return a value.
/// </summary>
public interface ICommand : IRequest<Result>
{
}
