namespace BSX.Modules.Identity.Infrastructure.Persistence.Outbox;

/// <summary>Well-known outbox processing states.</summary>
public static class OutboxMessageStatus
{
    /// <summary>Awaiting processing.</summary>
    public const string Pending = "Pending";

    /// <summary>Successfully processed.</summary>
    public const string Processed = "Processed";

    /// <summary>Processing failed and may be retried.</summary>
    public const string Failed = "Failed";

    /// <summary>Exhausted retries; requires operator intervention.</summary>
    public const string DeadLettered = "DeadLettered";

    /// <summary>Domain event kind.</summary>
    public const string DomainKind = "Domain";

    /// <summary>Integration event kind.</summary>
    public const string IntegrationKind = "Integration";
}
