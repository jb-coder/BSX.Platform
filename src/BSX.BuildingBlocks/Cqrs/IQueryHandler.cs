using BSX.SharedKernel.Results;

namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// Handles a query that returns a value.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The type of the produced value.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
