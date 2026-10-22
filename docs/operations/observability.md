# Observability

Platform instrumentation for the modular monolith. All instruments are defined once in
`BSX.BuildingBlocks.Diagnostics.PlatformDiagnostics` (meter/source `BSX.Platform`) and exported
through OpenTelemetry when `OpenTelemetry:OtlpEndpoint` is configured.

## Structured logging

- Serilog is the only logging backend; no string-concatenated messages.
- Domain event and outbox logs carry `EventId`, `EventType`, `Module`, `RetryCount` and the
  OpenTelemetry trace/span when available.
- Payloads are never logged. Events are identified by type and id only.

## Traces

| Activity source | Span | Purpose |
| --- | --- | --- |
| `BSX.Platform` | `outbox.dispatch_domain_events` | One post-commit domain dispatch pass. |
| `BSX.Platform` | `outbox.recovery_pass` | One background outbox recovery pass. |

ASP.NET Core and `HttpClient` instrumentation remain enabled. Trace ids link logs to spans.

## Metrics

| Metric | Type | Meaning |
| --- | --- | --- |
| `bsx.domain.events.dispatched` | Counter | Domain events successfully dispatched. |
| `bsx.domain.events.failed` | Counter | Domain events with at least one failing handler. |
| `bsx.domain.events.dead_lettered` | Counter | Domain events moved to the dead-letter state. |
| `bsx.domain.event_handlers.executed` | Counter | Handler executions. |
| `bsx.domain.event_handlers.failed` | Counter | Handler failures. |
| `bsx.domain.event_handler.duration` | Histogram (ms) | Handler duration. |
| `bsx.outbox.messages.persisted` | Counter | Outbox rows written with aggregate state. |
| `bsx.outbox.messages.processed` | Counter | Outbox rows processed successfully. |
| `bsx.outbox.messages.failed` | Counter | Outbox processing failures. |
| `bsx.outbox.messages.dead_lettered` | Counter | Rows that exhausted retries. |
| `bsx.outbox.messages.requeued` | Counter | Stale claims requeued after a crash. |
| `bsx.outbox.backlog` | Observable gauge | Pending rows across all module stores. |
| `bsx.permissions.synchronizations` | Counter | Permission synchronization runs. |
| `bsx.permissions.synchronization.failures` | Counter | Failed synchronization runs. |
| `bsx.permissions.added` | Counter | Permissions granted by reconciliation. |
| `bsx.permissions.removed` | Counter | Stale permissions revoked. |

## Suggested alerts

- `bsx.outbox.backlog > 0` for more than 5 minutes.
- Any increase in `bsx.outbox.messages.dead_lettered` or `bsx.domain.events.dead_lettered`.
- `bsx.domain.event_handlers.failed` rate above the baseline.
- `bsx.permissions.synchronization.failures > 0` (usually a catalog conflict or schema issue).

## Configuration

```json
{
  "OpenTelemetry": { "OtlpEndpoint": "http://otel-collector:4317" },
  "Outbox": {
    "Enabled": true,
    "ProcessingEnabled": true,
    "PollInterval": "00:00:05",
    "BatchSize": 100,
    "MaxRetryCount": 10,
    "BaseBackoff": "00:00:05",
    "StaleProcessingAfter": "00:05:00"
  }
}
```

The platform meter and activity source are added in `BSX.Web/Program.cs` via
`AddMeter(PlatformDiagnostics.MeterName)` and `AddSource(PlatformDiagnostics.ActivitySourceName)`.
