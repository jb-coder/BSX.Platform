using BSX.BuildingBlocks.Persistence.Outbox;

namespace BSX.Persistence.Outbox;

/// <summary>
/// A transactional outbox row (ADR-009). Domain rows are dispatched in-process; integration rows
/// are published to a broker by the background processor. The row is written in the same database
/// transaction as the aggregate state, so a crash can never lose an event.
/// </summary>
public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="OutboxMessage"/> class.</summary>
    public OutboxMessage(
        Guid id,
        Guid eventId,
        string kind,
        string eventType,
        string payload,
        DateTimeOffset occurredOnUtc,
        DateTimeOffset createdOnUtc,
        Guid? correlationId = null,
        Guid? tenantId = null)
    {
        Id = id;
        EventId = eventId;
        Kind = kind;
        EventType = eventType;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
        CreatedOnUtc = createdOnUtc;
        CorrelationId = correlationId;
        TenantId = tenantId;
        Status = OutboxMessageStatus.Pending;
    }

    /// <summary>Gets the surrogate key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the event identifier (idempotency key).</summary>
    public Guid EventId { get; private set; }

    /// <summary>Gets the kind: <c>Domain</c> or <c>Integration</c>.</summary>
    public string Kind { get; private set; } = null!;

    /// <summary>Gets the fully qualified event type.</summary>
    public string EventType { get; private set; } = null!;

    /// <summary>Gets the serialized payload.</summary>
    public string Payload { get; private set; } = null!;

    /// <summary>Gets the occurrence timestamp.</summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <summary>Gets the processed timestamp, if any.</summary>
    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    /// <summary>Gets the timestamp of the last failed attempt, if any.</summary>
    public DateTimeOffset? LastAttemptOnUtc { get; private set; }

    /// <summary>Gets when the next retry is eligible, if any.</summary>
    public DateTimeOffset? NextAttemptOnUtc { get; private set; }

    /// <summary>Gets when the row was claimed, if any.</summary>
    public DateTimeOffset? ProcessingStartedOnUtc { get; private set; }

    /// <summary>Gets the processing status.</summary>
    public string Status { get; private set; } = null!;

    /// <summary>Gets the number of failed processing attempts.</summary>
    public int RetryCount { get; private set; }

    /// <summary>Gets the last error, if any.</summary>
    public string? LastError { get; private set; }

    /// <summary>Gets the correlation identifier.</summary>
    public Guid? CorrelationId { get; private set; }

    /// <summary>Gets the tenant identifier (ADR-013).</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Marks the row as claimed.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public void MarkProcessing(DateTimeOffset nowUtc)
    {
        Status = OutboxMessageStatus.Processing;
        ProcessingStartedOnUtc = nowUtc;
    }

    /// <summary>Marks the row as processed.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public void MarkProcessed(DateTimeOffset nowUtc)
    {
        ProcessedOnUtc = nowUtc;
        Status = OutboxMessageStatus.Processed;
        LastError = null;
        NextAttemptOnUtc = null;
        ProcessingStartedOnUtc = null;
    }

    /// <summary>Records a failed attempt and schedules a retry or dead-letters the row.</summary>
    /// <param name="error">The failure message.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="maxRetryCount">The maximum number of attempts.</param>
    /// <param name="baseBackoff">The base backoff; doubles per attempt.</param>
    public void RecordFailure(string error, DateTimeOffset nowUtc, int maxRetryCount, TimeSpan baseBackoff)
    {
        RetryCount++;
        LastError = error;
        LastAttemptOnUtc = nowUtc;
        ProcessingStartedOnUtc = null;

        if (RetryCount >= maxRetryCount)
        {
            Status = OutboxMessageStatus.DeadLettered;
            NextAttemptOnUtc = null;
            return;
        }

        Status = OutboxMessageStatus.Failed;
        double multiplier = Math.Pow(2, Math.Max(0, RetryCount - 1));
        NextAttemptOnUtc = nowUtc.AddTicks((long)(baseBackoff.Ticks * multiplier));
    }

    /// <summary>Requeues a stale claim.</summary>
    public void Requeue()
    {
        Status = OutboxMessageStatus.Pending;
        ProcessingStartedOnUtc = null;
    }

    /// <summary>Projects the entity into a storage-neutral envelope.</summary>
    /// <param name="moduleName">The owning module.</param>
    public OutboxEnvelope ToEnvelope(string moduleName) => new(
        EventId,
        Kind,
        EventType,
        Payload,
        OccurredOnUtc,
        RetryCount,
        TenantId,
        moduleName);
}
