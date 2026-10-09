using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.BuildingBlocks.Cqrs;

/// <summary>
/// In-process implementation of <see cref="ISender"/>.
/// Resolves the handler and the pipeline behaviors for the concrete request type and
/// caches the resulting dispatcher so that reflection is only performed once per request type.
/// </summary>
internal sealed class Sender : ISender
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapper> Wrappers = new();

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sender"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers and behaviors.</param>
    public Sender(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        RequestHandlerWrapper wrapper = Wrappers.GetOrAdd(request.GetType(), CreateWrapper);
        object? response = await wrapper.HandleAsync(request, _serviceProvider, cancellationToken).ConfigureAwait(false);
        return (TResponse)response!;
    }

    private static RequestHandlerWrapper CreateWrapper(Type requestType)
    {
        Type responseType = requestType
            .GetInterfaces()
            .First(static type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequest<>))
            .GetGenericArguments()[0];

        Type wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);
        return (RequestHandlerWrapper)Activator.CreateInstance(wrapperType)!;
    }

    private abstract class RequestHandlerWrapper
    {
        public abstract Task<object?> HandleAsync(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    private sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper
        where TRequest : IRequest<TResponse>
    {
        public override async Task<object?> HandleAsync(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            IRequestHandler<TRequest, TResponse> handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            RequestHandlerDelegate<TResponse> pipeline = () => handler.HandleAsync(typedRequest, cancellationToken);

            foreach (IPipelineBehavior<TRequest, TResponse> behavior in serviceProvider
                .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                .Reverse())
            {
                RequestHandlerDelegate<TResponse> next = pipeline;
                IPipelineBehavior<TRequest, TResponse> current = behavior;
                pipeline = () => current.HandleAsync(typedRequest, next, cancellationToken);
            }

            TResponse response = await pipeline().ConfigureAwait(false);
            return response;
        }
    }
}
