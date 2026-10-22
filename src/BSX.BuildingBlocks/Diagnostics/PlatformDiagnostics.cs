using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BSX.BuildingBlocks.Diagnostics;

/// <summary>
/// Central OpenTelemetry instrumentation for platform concerns (ADR-009/ADR-018).
/// Modules and infrastructure must use these instruments instead of ad-hoc meters so that
/// dashboards and alerts remain stable across the platform.
/// </summary>
public static class PlatformDiagnostics
{
    /// <summary>The meter name exported by the platform.</summary>
    public const string MeterName = "BSX.Platform";

    /// <summary>The activity source name exported by the platform.</summary>
    public const string ActivitySourceName = "BSX.Platform";

    /// <summary>The platform meter.</summary>
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    /// <summary>The platform activity source used for outbox and event dispatch traces.</summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    /// <summary>Total domain events dispatched.</summary>
    public static readonly Counter<long> DomainEventsDispatched =
        Meter.CreateCounter<long>("bsx.domain.events.dispatched", "events", "Domain events dispatched to handlers.");

    /// <summary>Total domain event dispatch failures.</summary>
    public static readonly Counter<long> DomainEventsFailed =
        Meter.CreateCounter<long>("bsx.domain.events.failed", "events", "Domain events with at least one failing handler.");

    /// <summary>Total domain events dead-lettered.</summary>
    public static readonly Counter<long> DomainEventsDeadLettered =
        Meter.CreateCounter<long>("bsx.domain.events.dead_lettered", "events", "Domain events moved to the dead-letter state.");

    /// <summary>Total domain event handler executions.</summary>
    public static readonly Counter<long> DomainEventHandlersExecuted =
        Meter.CreateCounter<long>("bsx.domain.event_handlers.executed", "handlers", "Domain event handler executions.");

    /// <summary>Total domain event handler failures.</summary>
    public static readonly Counter<long> DomainEventHandlersFailed =
        Meter.CreateCounter<long>("bsx.domain.event_handlers.failed", "handlers", "Domain event handler failures.");

    /// <summary>Duration of a single domain event handler execution.</summary>
    public static readonly Histogram<double> DomainEventHandlerDuration =
        Meter.CreateHistogram<double>("bsx.domain.event_handler.duration", "ms", "Domain event handler duration.");

    /// <summary>Total outbox messages persisted.</summary>
    public static readonly Counter<long> OutboxMessagesPersisted =
        Meter.CreateCounter<long>("bsx.outbox.messages.persisted", "messages", "Outbox messages persisted with aggregate state.");

    /// <summary>Total outbox messages processed.</summary>
    public static readonly Counter<long> OutboxMessagesProcessed =
        Meter.CreateCounter<long>("bsx.outbox.messages.processed", "messages", "Outbox messages processed successfully.");

    /// <summary>Total outbox message processing failures.</summary>
    public static readonly Counter<long> OutboxMessagesFailed =
        Meter.CreateCounter<long>("bsx.outbox.messages.failed", "messages", "Outbox message processing failures.");

    /// <summary>Total outbox messages dead-lettered.</summary>
    public static readonly Counter<long> OutboxMessagesDeadLettered =
        Meter.CreateCounter<long>("bsx.outbox.messages.dead_lettered", "messages", "Outbox messages moved to the dead-letter state.");

    /// <summary>Total outbox messages republished after a stale claim.</summary>
    public static readonly Counter<long> OutboxMessagesRequeued =
        Meter.CreateCounter<long>("bsx.outbox.messages.requeued", "messages", "Stale outbox claims requeued for retry.");

    /// <summary>Total permission synchronization runs.</summary>
    public static readonly Counter<long> PermissionSynchronizations =
        Meter.CreateCounter<long>("bsx.permissions.synchronizations", "runs", "Permission catalog synchronization runs.");

    /// <summary>Total permission synchronization failures.</summary>
    public static readonly Counter<long> PermissionSynchronizationFailures =
        Meter.CreateCounter<long>("bsx.permissions.synchronization.failures", "runs", "Permission catalog synchronization failures.");

    /// <summary>Total permissions granted during reconciliation.</summary>
    public static readonly Counter<long> PermissionsAdded =
        Meter.CreateCounter<long>("bsx.permissions.added", "permissions", "Permissions granted by synchronization.");

    /// <summary>Total stale permissions revoked during reconciliation.</summary>
    public static readonly Counter<long> PermissionsRemoved =
        Meter.CreateCounter<long>("bsx.permissions.removed", "permissions", "Stale permissions revoked by synchronization.");

    private static long _outboxBacklog;

    static PlatformDiagnostics()
    {
        _ = Meter.CreateObservableGauge(
            "bsx.outbox.backlog",
            static () => Interlocked.Read(ref _outboxBacklog),
            "messages",
            "Pending outbox messages.");
    }

    /// <summary>Records the current outbox backlog across all module stores.</summary>
    /// <param name="backlog">The pending message count.</param>
    public static void SetOutboxBacklog(long backlog) => Interlocked.Exchange(ref _outboxBacklog, backlog);
}
