namespace BSX.Modules.Identity.Infrastructure.Persistence.Outbox;

/// <summary>
/// A transactional outbox row (ADR-009). Domain rows are dispatched in-process; integration rows
/// are published to a broker by the background processor.
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
        bool isSensitive,
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
        IsSensitive = isSensitive;
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

    /// <summary>Gets or sets the serialized payload (encrypted when sensitive).</summary>
    public string Payload { get; set; } = null!;

    /// <summary>Gets a value indicating whether the payload is sensitive and stored encrypted.</summary>
    public bool IsSensitive { get; private set; }

    /// <summary>Gets the occurrence timestamp.</summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <summary>Gets or sets the processed timestamp, if any.</summary>
    public DateTimeOffset? ProcessedOnUtc { get; set; }

    /// <summary>Gets or sets the processing status.</summary>
    public string Status { get; set; } = null!;

    /// <summary>Gets or sets the number of processing attempts.</summary>
    public int RetryCount { get; set; }

    /// <summary>Gets or sets the last error, if any.</summary>
    public string? LastError { get; set; }

    /// <summary>Gets the correlation identifier.</summary>
    public Guid? CorrelationId { get; private set; }

    /// <summary>Gets the tenant identifier (reserved; ADR-013).</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Marks the row as processed.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public void MarkProcessed(DateTimeOffset nowUtc)
    {
        ProcessedOnUtc = nowUtc;
        Status = OutboxMessageStatus.Processed;
    }

    /// <summary>Records a processing failure.</summary>
    /// <param name="error">The failure message.</param>
    public void RecordFailure(string error)
    {
        RetryCount++;
        LastError = error;
        Status = OutboxMessageStatus.Failed;
    }
}
