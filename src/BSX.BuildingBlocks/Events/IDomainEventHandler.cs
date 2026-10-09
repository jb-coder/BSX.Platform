using BSX.SharedKernel.Primitives;

namespace BSX.BuildingBlocks.Events;

/// <summary>
/// Handles a specific type of domain event.
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type.</typeparam>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
