using System.Text.Json;
using BSX.BuildingBlocks.Diagnostics;
using BSX.BuildingBlocks.Persistence.Outbox;
using BSX.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace BSX.Persistence.Outbox;

/// <summary>
/// Persists pending domain events as outbox rows in the same <c>SaveChanges</c> transaction as the
/// aggregate state (ADR-009). A crash after commit therefore never loses an event. Rows are
/// deduplicated by <see cref="OutboxMessage.EventId"/> so repeated saves cannot create duplicates.
/// </summary>
public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;
    private readonly OutboxOptions _options;

    /// <summary>Initializes a new instance of the <see cref="OutboxSaveChangesInterceptor"/> class.</summary>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="options">The outbox options.</param>
    public OutboxSaveChangesInterceptor(TimeProvider timeProvider, IOptions<OutboxOptions> options)
    {
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (!_options.Enabled || context is not IOutboxDbContext outboxContext)
        {
            return;
        }

        HashSet<Guid> knownEventIds = outboxContext.OutboxMessages
            .Local
            .Select(message => message.EventId)
            .ToHashSet();

        // Materialize: adding outbox rows mutates the change tracker while we enumerate.
        foreach (var entry in context.ChangeTracker.Entries<IHasDomainEvents>().ToArray())
        {
            foreach (IDomainEvent domainEvent in entry.Entity.DomainEvents)
            {
                if (!knownEventIds.Add(domainEvent.EventId))
                {
                    continue;
                }

                Type eventType = domainEvent.GetType();
                string payload = JsonSerializer.Serialize(domainEvent, eventType);
                DateTimeOffset now = _timeProvider.GetUtcNow();

                outboxContext.OutboxMessages.Add(new OutboxMessage(
                    Guid.NewGuid(),
                    domainEvent.EventId,
                    OutboxKinds.Domain,
                    eventType.FullName ?? eventType.Name,
                    payload,
                    domainEvent.OccurredOnUtc,
                    now));

                PlatformDiagnostics.OutboxMessagesPersisted.Add(
                    1,
                    new KeyValuePair<string, object?>("module", outboxContext.ModuleName),
                    new KeyValuePair<string, object?>("kind", OutboxKinds.Domain));
            }
        }
    }
}
