using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// Marker abstraction for a command that modifies state and returns a value on success.
/// </summary>
/// <typeparam name="TResponse">The type of the produced value.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
