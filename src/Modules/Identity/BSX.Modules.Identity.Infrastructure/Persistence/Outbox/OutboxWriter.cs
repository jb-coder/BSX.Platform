using System.Text.Json;
using BSX.BuildingBlocks.Events;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.Persistence.Outbox;

namespace BSX.Modules.Identity.Infrastructure.Persistence.Outbox;

internal sealed class OutboxWriter : IOutboxWriter
{
    private readonly IdentityDbContext _context;
    private readonly TimeProvider _timeProvider;

    public OutboxWriter(IdentityDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public void Enqueue(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        Type eventType = integrationEvent.GetType();
        string payload = JsonSerializer.Serialize(integrationEvent, eventType);
        string typeName = eventType.FullName ?? eventType.Name;

        var message = new OutboxMessage(
            Guid.NewGuid(),
            integrationEvent.EventId,
            OutboxKinds.Integration,
            typeName,
            payload,
            integrationEvent.OccurredOnUtc,
            _timeProvider.GetUtcNow());

        _context.OutboxMessages.Add(message);
    }
}
