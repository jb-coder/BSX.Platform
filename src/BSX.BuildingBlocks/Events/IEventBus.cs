namespace BSX.BuildingBlocks.Events;

/// <summary>
/// Publishes integration events to the rest of the platform.
/// The default implementation will be replaced by an Outbox-backed publisher that targets
/// RabbitMQ, Azure Service Bus or Kafka without changing the calling modules.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an integration event.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The integration event type.</typeparam>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent;
}
