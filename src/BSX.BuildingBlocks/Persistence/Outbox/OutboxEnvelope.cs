namespace BSX.BuildingBlocks.Persistence.Outbox;

/// <summary>
/// A persisted outbox message projected into a storage-neutral envelope for dispatch
/// (ADR-009). Store implementations return envelopes; the dispatcher never references EF Core.
/// </summary>
/// <param name="EventId">The idempotency key of the event.</param>
/// <param name="Kind">The outbox kind (<see cref="OutboxKinds.Domain"/> or <see cref="OutboxKinds.Integration"/>).</param>
/// <param name="EventType">The fully qualified event type.</param>
/// <param name="Payload">The serialized event payload.</param>
/// <param name="OccurredOnUtc">When the event occurred.</param>
/// <param name="RetryCount">How many processing attempts have failed.</param>
/// <param name="TenantId">The tenant that raised the event, when known.</param>
/// <param name="ModuleName">The owning module.</param>
public sealed record OutboxEnvelope(
    Guid EventId,
    string Kind,
    string EventType,
    string Payload,
    DateTimeOffset OccurredOnUtc,
    int RetryCount,
    Guid? TenantId,
    string ModuleName);

/// <summary>Well-known outbox message kinds (ADR-009).</summary>
public static class OutboxKinds
{
    /// <summary>An intra-module domain event dispatched in-process after commit.</summary>
    public const string Domain = "Domain";

    /// <summary>An inter-module integration event published to a broker.</summary>
    public const string Integration = "Integration";
}
