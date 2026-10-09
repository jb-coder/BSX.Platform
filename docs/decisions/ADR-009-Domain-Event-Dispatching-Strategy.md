# ADR-009 - Domain Event Dispatching Strategy

## Status

Accepted

---

## Context

BSX Platform is a modular monolith built on ASP.NET Core (.NET 10), Entity Framework Core and
PostgreSQL. Modules must remain loosely coupled and communicate only through events and contracts
(architecture-skill Rule 7, [module-boundaries.md](../architecture/module-boundaries.md)).

The foundation introduced `IDomainEvent`, `AggregateRoot` event collection, `IDomainEventDispatcher`
and integration-event abstractions, but **no dispatch point was defined**. Domain events were
collected and then lost — a silent correctness gap identified during the foundation review.

The following constraints apply:

- Commands that modify business data run inside a transaction; queries never do (database-skill).
- Expected failures are values, not exceptions (Rule 5).
- The architecture must be Outbox-ready for RabbitMQ, Azure Service Bus or Kafka (database-skill).
- Architectural simplicity is preferred over purity (Rule 10): the first stage must be in-process
  and operationally light, without blocking future broker adoption.
- Business logic must not live in endpoints or infrastructure (Rule 2).

This ADR defines the single, official strategy for raising, persisting, dispatching and observing
domain events.

---

## Decision

Adopt a **two-tier, persistence-first, post-commit, in-process** dispatching strategy backed by a
**transactional Outbox**.

### Event Taxonomy

Two distinct event kinds exist. They must never be conflated.

| Aspect | Domain Event | Integration Event |
| --- | --- | --- |
| Scope | Inside one module | Across module boundaries |
| Transport | In-process | Outbox → message broker |
| Timing | After commit, awaited | Asynchronous, background |
| Contract | Internal, may change freely | Published contract, versioned |
| Raising | `AggregateRoot.RaiseDomainEvent` | Produced from a domain event or handler |
| Delivery | At-least-once | At-least-once |

**Rule:** domain events never cross a module boundary. To cross a boundary, a domain event is
translated into one or more integration events. Integration events are the only permitted
inter-module communication channel.

---

### 1. Event Lifecycle

```text
Raised ──► Collected ──► Persisted ──► Dispatched ──► Handled ──► Completed
   │            │             │              │            │
   │            │             │              │            └──► Failed ──► Retry ──┐
   │            │             │              │                                    │
   │            │             │              └──────────────────────────────◄─────┘
   │            │             │
   │            │             └── persisted atomically with aggregate state
   │            └── collected at persistence time
   └── recorded by the aggregate during a command
```

States:

1. **Raised** — an aggregate records an `IDomainEvent` through `RaiseDomainEvent`. No I/O occurs.
2. **Collected** — at persistence time the unit of work scans tracked aggregates and gathers
   pending events. Events are never written directly by domain code.
3. **Persisted** — collected events are written to the `outbox.messages` table **in the same
   transaction as the aggregate state**. State and events are atomic.
4. **Dispatched** — after the transaction commits, the in-process dispatcher loads the persisted
   domain events and invokes every registered handler.
5. **Handled** — handlers execute intra-module side effects. A handler may produce integration
   events, which are enqueued to the outbox in their own transaction.
6. **Completed** — all handlers succeeded; the outbox row is marked processed and the aggregate's
   in-memory events are cleared.
7. **Failed / Retry / Dead-lettered** — any handler failure is isolated, recorded and retried;
   exhaustion moves the row to the dead-letter state (see §5).

Two independent loops use the table:

- **Domain loop** (synchronous, in-process): an outbox row with `kind = Domain` is dispatched
  immediately after commit within the request scope.
- **Integration loop** (asynchronous, background): an outbox row with `kind = Integration` is
  published to the broker by the outbox publisher.

---

### 2. Dispatch Point

The dispatch point is the **unit of work**, after a successful commit — never inside the
transaction and never in an endpoint or handler.

Rules:

- Events are dispatched **only after the transaction that persisted them has committed**. Acting
  on uncommitted or rolled-back state is forbidden.
- The unit of work exposes a single seam, `SaveChangesAndDispatchAsync`, which commits state and
  events and then triggers dispatch. This is the only entry point the command pipeline uses.
- The command **transaction behavior** (pipeline) calls this seam; query handlers never dispatch.
- Dispatch runs in the **same request scope** as the command so that handlers can reuse the
  scoped `DbContext` and the current user/correlation context.
- No handler may rely on an ambient transaction during domain dispatch — the business transaction
  has already committed. A handler that needs to write must persist through the unit of work,
  which starts its own transaction.

---

### 3. SaveChanges Integration

Persistence integration is centralized in the module `DbContext` (or a shared
`SaveChangesInterceptor`). No module implements this independently.

During `SaveChanges` / `SaveChangesAsync`:

1. **Pre-save stamping** — auditing fields (`CreatedOnUtc`, `CreatedBy`, `UpdatedOnUtc`,
   `UpdatedBy`) and soft-delete fields are applied. Uses the current user and `TimeProvider`.
2. **Collection** — the change tracker is scanned for tracked aggregates holding pending domain
   events. This requires a non-generic marker (`IHasDomainEvents`) because `AggregateRoot<TId>`
   is generic (see Required Kernel Change).
3. **Translation (optional)** — domain events that must cross a module boundary are translated
   into integration events at this point, so they persist atomically with state.
4. **Persistence** — every event becomes an outbox row (`kind = Domain` and/or
   `kind = Integration`) added to the same `ChangeTracker`, guaranteeing a single atomic commit.
5. **Capture** — the collected domain events are returned to the unit of work for post-commit
   dispatch.
6. **Clear** — the aggregate's in-memory events are cleared only after the events are persisted,
   so a failed save does not lose them.

Entity Framework's default transactional behavior is retained: a single `SaveChanges` is atomic.
Commands that span multiple `SaveChanges` calls are wrapped in an explicit
`IDbContextTransaction` by the transaction behavior.

---

### 4. Handler Execution Model

- **In-process and awaited.** Domain events dispatch synchronously after commit so the request
  observes the outcome and tests remain deterministic.
- **Sequential, in registration order.** Handlers for the same event run in the order they were
  registered. No parallelism in stage 1 (Rule 10). Parallelism may be introduced later, grouped
  by event type, without changing the contract.
- **One handler set per event type**, resolved from DI. Zero handlers is valid.
- **Reentrancy guard.** Events raised by handlers are appended to a queue and processed in a
  subsequent pass. A configurable maximum depth (default small) bounds cascades; exceeding it is
  logged as critical to prevent infinite loops.
- **Ordering guarantee.** Per aggregate/stream ordering is preserved. Global ordering across
  aggregates is **not** guaranteed.
- **Idempotency.** Every handler must be idempotent, because delivery is at-least-once. Handlers
  that perform external side effects must use an idempotency key derived from `EventId`.
- **Handler placement.** Domain event handlers belong to the module that owns the event. They must
  not reference other modules' internals; cross-module reactions are integration events.

---

### 5. Failure Isolation

Because dispatch happens after commit, a handler failure can never roll back business state. The
strategy is therefore at-least-once with isolation, retry and dead-lettering.

- **Per-handler isolation.** Each handler runs in its own try/catch. A failure never aborts the
  remaining handlers or the current event's other handlers.
- **Never silent.** Every failure is logged with `EventId`, `EventType`, `AggregateId`,
  `CorrelationId` and the handler name, and increments a failure metric.
- **State is not rolled back.** The committed transaction stands. Failure is an operational
  concern, not a client error.
- **Request semantics.** By default, a post-commit domain dispatch failure does **not** fail the
  HTTP request, because the command already succeeded. A module may opt in to surface a critical
  failure explicitly.
- **Retries.** Failed domain rows record the error and retry count and are retried by the outbox
  processor with exponential backoff. Integration rows follow the same policy after the broker
  publish fails.
- **Dead-lettering.** After `MaxRetryCount` attempts, a row is moved to the dead-letter state and
  surfaced for operator intervention. It is never deleted.
- **Poison events.** Events that repeatedly fail are quarantined so they cannot block the queue
  head.

---

### 6. Logging Strategy

Observability is mandatory, and logs must correlate across Serilog and OpenTelemetry.

- **Structured logging only** (Serilog). No string concatenation.
- **Lifecycle logs.** Collection (`Debug`), persistence (`Debug`), dispatch start/end (`Debug`),
  handler success (`Debug`), handler failure (`Error`).
- **Correlation.** Every log carries `CorrelationId`, `EventId`, `EventType`, `AggregateId`,
  `Handler`, and the OpenTelemetry `TraceId`/`SpanId` so logs and traces link.
- **No payloads by default.** Event payloads may contain personal data; they are never logged
  unless explicitly enabled per environment. Log identifiers, not content.
- **Metrics (OpenTelemetry).** Counters for events persisted, dispatched, failed, retried and
  dead-lettered; a histogram for handler duration; a gauge for outbox backlog.
- **Health.** Readiness reflects outbox backlog; sustained backlog beyond a threshold marks the
  service as degraded.

---

### 7. Future Outbox Compatibility

The design is broker-agnostic and migration-safe. Stage 1 processes `kind = Domain` rows
in-process and leaves the integration loop dormant until a broker is configured.

Outbox table (conceptual schema):

| Column | Purpose |
| --- | --- |
| `Id` | Surrogate key, ordered for FIFO. |
| `EventId` | Domain/integration event identifier (idempotency key). |
| `Kind` | `Domain` or `Integration`. |
| `EventType` | Fully qualified event type, for deserialization and routing. |
| `Payload` | Serialized event, stored as PostgreSQL `jsonb`. |
| `OccurredOnUtc` | When the event occurred. |
| `CreatedOnUtc` | When the row was written. |
| `ProcessedOnUtc` | When dispatch/publish completed; null while pending. |
| `Status` | `Pending`, `Processed`, `Failed`, `DeadLettered`. |
| `RetryCount` | Number of attempts. |
| `LastError` | Last failure message. |
| `CorrelationId` | Request/event correlation. |
| `TenantId` | Nullable; present from day one for multi-tenancy readiness. |

Compatibility guarantees:

- **Atomicity.** The outbox row is written in the same transaction as the aggregate state. There
  is no dual-write hazard.
- **Background processor.** A hosted service polls pending `Integration` rows (configurable
  interval) in batches and publishes them through the `IEventBus` abstraction. PostgreSQL
  `LISTEN/NOTIFY` may replace polling later as an optimization; the contract does not change.
- **Broker abstraction.** `IEventBus` isolates RabbitMQ, Azure Service Bus and Kafka behind one
  envelope. No module references a broker SDK.
- **Idempotent consumers.** At-least-once delivery requires an inbox/deduplication table on the
  consuming side, keyed by `EventId`, to reach effectively-once processing.
- **Multi-tenancy.** `TenantId` is reserved now so tenant isolation needs no schema migration.
- **Retention.** Processed rows are archived and purged on a configurable schedule; dead-letter
  rows are retained.

---

## Required Kernel Change

The persistence layer cannot discover pending events through the generic `AggregateRoot<TId>`.
A non-generic marker is required:

- `IHasDomainEvents` (with `IReadOnlyCollection<IDomainEvent> DomainEvents` and
  `ClearDomainEvents()`), implemented by `AggregateRoot<TId>`.

This is a supporting type for `AggregateRoot`, consistent with the spirit of Rule 9 (the kernel
stays minimal). Event timestamps should also resolve through `TimeProvider` rather than
`DateTimeOffset.UtcNow` for deterministic testing.

---

## Alternatives Considered

### Dispatch inside the transaction

Pros

- Simplest mental model; events visible immediately.

Cons

- Handlers act on uncommitted state that may roll back.
- Holds the transaction open during handler I/O; long locks and deadlock risk.
- Violates the atomicity guarantee.

### In-memory only, no outbox

Pros

- Least infrastructure.

Cons

- Events are lost on crash or if the process dies after commit.
- No audit trail, no retry, no dead-letter.
- Not Outbox-ready; a future migration would be invasive.

### Dedicated messaging framework (MassTransit / NServiceBus / MediatR Outbox)

Pros

- Feature-rich, proven.

Cons

- Heavy dependency and learning surface for a modular monolith stage 1.
- License and governance concerns (consistent with ADR-007).
- Violates "recommend technology only for measurable value."

### Direct synchronous cross-module calls

Pros

- Trivial.

Cons

- Tight coupling; violates Rule 1 and Rule 7.
- Breaks independent evolvability of modules.

---

## Consequences

### Positive

- No silent event loss: events are persisted atomically and are recoverable after a crash.
- Domain code stays pure: aggregates only record events; infrastructure persists and dispatches.
- Single, testable seam for dispatch.
- Failure isolation and retry without corrupting business state.
- Smooth path to a broker: only the integration loop activates; contracts do not change.
- Multi-tenancy and observability are prepared from the start.

### Negative

- Additional table and background processor to operate and monitor.
- At-least-once delivery forces idempotent handlers.
- Slightly more database round-trips than pure in-memory dispatch.
- Eventual consistency between modules (accepted and intended).

---

## Notes

Configuration knobs (names indicative, values tuned per environment):

- `Outbox:PollIntervalSeconds`, `Outbox:BatchSize`
- `Outbox:MaxRetryCount`, `Outbox:BackoffSeconds`
- `DomainEvents:MaxDispatchDepth`
- `Outbox:ProcessedRetentionDays`

Related documents:

- [ADR-007 - In-Process CQRS Dispatcher](ADR-007-In-Process-CQRS-Dispatcher.md)
- [ADR-008 - Central Package Management and Build Governance](ADR-008-Central-Package-Management.md)
- [domain-event-dispatch.md](../diagrams/domain-event-dispatch.md)
- [building-blocks.md](../architecture/building-blocks.md)
