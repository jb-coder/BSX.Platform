using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// Marker abstraction for a query that reads state and never modifies it.
/// </summary>
/// <typeparam name="TResponse">The type of the produced value.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
