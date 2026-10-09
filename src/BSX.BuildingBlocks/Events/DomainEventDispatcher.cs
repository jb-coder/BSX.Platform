using System.Reflection;
using BSX.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.BuildingBlocks.Events;

/// <summary>
/// In-process implementation of <see cref="IDomainEventDispatcher"/>.
/// Resolves every <see cref="IDomainEventHandler{TDomainEvent}"/> registered for the runtime
/// type of each domain event.
/// </summary>
internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private const string HandleMethodName = "HandleAsync";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
    public DomainEventDispatcher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (IDomainEvent domainEvent in domainEvents)
        {
            Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            MethodInfo handleMethod = handlerType.GetMethod(HandleMethodName)!;

            foreach (object? handler in _serviceProvider.GetServices(handlerType))
            {
                if (handler is null)
                {
                    continue;
                }

                var task = (Task?)handleMethod.Invoke(handler, [domainEvent, cancellationToken]);
                if (task is not null)
                {
                    await task.ConfigureAwait(false);
                }
            }
        }
    }
}
