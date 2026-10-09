namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// Represents the continuation to the next step of the request pipeline.
/// </summary>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
/// <returns>The response produced by the remaining pipeline.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
