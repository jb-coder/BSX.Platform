namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// Marker abstraction for a request that produces a response.
/// Commands and queries derive from this abstraction.
/// </summary>
/// <typeparam name="TResponse">The type of the response produced by the request.</typeparam>
public interface IRequest<out TResponse>
{
}
