namespace BSX.Persistence.Outbox;

/// <summary>Configuration for the transactional outbox processor (ADR-009).</summary>
public sealed class OutboxOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Outbox";

    /// <summary>Gets or sets a value indicating whether domain events are written to the outbox.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the background recovery loop runs.</summary>
    public bool ProcessingEnabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether integration rows are published to the broker.</summary>
    public bool IntegrationPublishingEnabled { get; set; }

    /// <summary>Gets or sets the poll interval for the recovery loop.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets the maximum number of rows processed per poll and module.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Gets or sets the maximum number of failed attempts before dead-lettering.</summary>
    public int MaxRetryCount { get; set; } = 10;

    /// <summary>Gets or sets the base retry backoff.</summary>
    public TimeSpan BaseBackoff { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Gets or sets how long a claimed row may remain in processing before it is requeued.</summary>
    public TimeSpan StaleProcessingAfter { get; set; } = TimeSpan.FromMinutes(5);
}
