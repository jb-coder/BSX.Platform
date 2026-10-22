using BSX.BuildingBlocks.Diagnostics;
using BSX.BuildingBlocks.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BSX.Persistence.Outbox;

/// <summary>
/// EF Core implementation of <see cref="IOutboxStore"/> for a module context. Claims are performed
/// with a conditional atomic <c>UPDATE</c> (one winner), which gives exactly-once dispatch per
/// attempt across concurrent workers and instances without provider-specific SQL.
/// </summary>
/// <typeparam name="TContext">The module persistence context.</typeparam>
public sealed class EfOutboxStore<TContext> : IOutboxStore
    where TContext : DbContext, IOutboxDbContext
{
    private readonly TContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly OutboxOptions _options;

    /// <summary>Initializes a new instance of the <see cref="EfOutboxStore{TContext}"/> class.</summary>
    /// <param name="context">The module context.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="options">The outbox options.</param>
    public EfOutboxStore(TContext context, TimeProvider timeProvider, IOptions<OutboxOptions> options)
    {
        _context = context;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public string ModuleName => _context.ModuleName;

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxEnvelope>> ClaimAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        if (eventIds.Count == 0)
        {
            return [];
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        Guid[] ids = eventIds.Distinct().ToArray();

        List<OutboxMessage> candidates = await _context.OutboxMessages
            .AsNoTracking()
            .Where(message => ids.Contains(message.EventId)
                && (message.Status == OutboxMessageStatus.Pending
                    || message.Status == OutboxMessageStatus.Failed))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var claimed = new List<OutboxEnvelope>(candidates.Count);

        foreach (OutboxMessage candidate in candidates)
        {
            // Evaluate retry eligibility in memory so the same code path works on SQLite and
            // PostgreSQL (SQLite cannot order DateTimeOffset values in SQL).
            if (candidate.Status == OutboxMessageStatus.Failed
                && candidate.NextAttemptOnUtc is not null
                && candidate.NextAttemptOnUtc > now)
            {
                continue;
            }

            Guid messageId = candidate.Id;
            int affected = await _context.OutboxMessages
                .Where(message => message.Id == messageId
                    && (message.Status == OutboxMessageStatus.Pending
                        || message.Status == OutboxMessageStatus.Failed))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.Status, OutboxMessageStatus.Processing)
                        .SetProperty(message => message.ProcessingStartedOnUtc, now),
                    cancellationToken)
                .ConfigureAwait(false);

            if (affected == 1)
            {
                claimed.Add(candidate.ToEnvelope(ModuleName));
            }
        }

        return claimed;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxEnvelope>> ReadPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        List<OutboxMessage> messages = await _context.OutboxMessages
            .AsNoTracking()
            .Where(message => message.Status == OutboxMessageStatus.Pending
                || message.Status == OutboxMessageStatus.Failed)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return messages
            .Where(message => message.Status == OutboxMessageStatus.Pending
                || message.NextAttemptOnUtc is null
                || message.NextAttemptOnUtc <= now)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(batchSize)
            .Select(message => message.ToEnvelope(ModuleName))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task MarkProcessedAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken = default)
    {
        if (eventIds.Count == 0)
        {
            return;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        Guid[] ids = eventIds.Distinct().ToArray();

        await _context.OutboxMessages
            .Where(message => ids.Contains(message.EventId))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Status, OutboxMessageStatus.Processed)
                    .SetProperty(message => message.ProcessedOnUtc, now)
                    .SetProperty(message => message.LastError, (string?)null)
                    .SetProperty(message => message.NextAttemptOnUtc, (DateTimeOffset?)null)
                    .SetProperty(message => message.ProcessingStartedOnUtc, (DateTimeOffset?)null),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(
        IReadOnlyCollection<Guid> eventIds,
        string error,
        CancellationToken cancellationToken = default)
    {
        if (eventIds.Count == 0)
        {
            return;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        Guid[] ids = eventIds.Distinct().ToArray();

        List<OutboxMessage> messages = await _context.OutboxMessages
            .Where(message => ids.Contains(message.EventId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (OutboxMessage message in messages)
        {
            message.RecordFailure(error, now, _options.MaxRetryCount, _options.BaseBackoff);

            PlatformDiagnostics.OutboxMessagesFailed.Add(
                1,
                new KeyValuePair<string, object?>("module", ModuleName));

            if (message.Status == OutboxMessageStatus.DeadLettered)
            {
                PlatformDiagnostics.OutboxMessagesDeadLettered.Add(
                    1,
                    new KeyValuePair<string, object?>("module", ModuleName));
                PlatformDiagnostics.DomainEventsDeadLettered.Add(1);
            }
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default)
        => _context.OutboxMessages
            .AsNoTracking()
            .CountAsync(
                message => message.Status == OutboxMessageStatus.Pending
                    || message.Status == OutboxMessageStatus.Failed,
                cancellationToken);

    /// <inheritdoc />
    public async Task<int> RequeueStaleAsync(
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset cutoff = _timeProvider.GetUtcNow().Subtract(staleAfter);

        List<OutboxMessage> processing = await _context.OutboxMessages
            .AsNoTracking()
            .Where(message => message.Status == OutboxMessageStatus.Processing)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<Guid> staleIds = processing
            .Where(message => message.ProcessingStartedOnUtc is null || message.ProcessingStartedOnUtc < cutoff)
            .Select(message => message.Id)
            .ToList();

        if (staleIds.Count == 0)
        {
            return 0;
        }

        int requeued = await _context.OutboxMessages
            .Where(message => staleIds.Contains(message.Id) && message.Status == OutboxMessageStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Status, OutboxMessageStatus.Pending)
                    .SetProperty(message => message.ProcessingStartedOnUtc, (DateTimeOffset?)null),
                cancellationToken)
            .ConfigureAwait(false);

        if (requeued > 0)
        {
            PlatformDiagnostics.OutboxMessagesRequeued.Add(
                requeued,
                new KeyValuePair<string, object?>("module", ModuleName));
        }

        return requeued;
    }
}
