namespace BSX.Persistence.Outbox;

/// <summary>Well-known outbox processing states (ADR-009).</summary>
public static class OutboxMessageStatus
{
    /// <summary>Awaiting processing or eligible for a retry.</summary>
    public const string Pending = "Pending";

    /// <summary>Claimed by a dispatcher or publisher; not yet completed.</summary>
    public const string Processing = "Processing";

    /// <summary>Successfully processed.</summary>
    public const string Processed = "Processed";

    /// <summary>Processing failed and a retry is scheduled.</summary>
    public const string Failed = "Failed";

    /// <summary>Exhausted retries; requires operator intervention.</summary>
    public const string DeadLettered = "DeadLettered";
}
