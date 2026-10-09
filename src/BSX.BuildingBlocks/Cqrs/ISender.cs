namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// Dispatches a request to its handler through the configured pipeline behaviors.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a command or query and returns its response.
    /// </summary>
    /// <typeparam name="TResponse">The response type produced by the request.</typeparam>
    /// <param name="request">The request to dispatch.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The response produced by the request handler.</returns>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
