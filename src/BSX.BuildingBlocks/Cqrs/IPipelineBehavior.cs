namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// A cross-cutting concern that wraps request handling.
/// Behaviors are executed in registration order, outermost first.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Executes the behavior.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">The continuation to the next pipeline step.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The response produced by the pipeline.</returns>
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default);
}
