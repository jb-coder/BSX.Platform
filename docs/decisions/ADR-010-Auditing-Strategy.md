# ADR-010 - Auditing Strategy

## Status

Accepted

---

## Context

The database-skill requires that every aggregate supports auditing with the standard fields
(`CreatedOnUtc`, `CreatedBy`, `UpdatedOnUtc`, `UpdatedBy`) and automatic population, and that
business data uses soft delete (`DeletedOnUtc`, `DeletedBy`, `IsDeleted`) with hard deletes kept
rare.

The foundation placed `IAuditableEntity` and `ISoftDeletable` in `BSX.BuildingBlocks.Persistence`.
That forces every domain entity that needs auditing to implement a BuildingBlocks interface,
which creates a **Domain → BuildingBlocks** dependency and inverts the intended dependency
direction. This was flagged as a High severity issue in the foundation review.

Additional requirements:

- Timestamps must be deterministic and testable (foundation review: `DateTimeOffset.UtcNow` is
  not testable).
- The "who" of an audit entry comes from the authenticated principal (ADR-012).
- The save pipeline is shared with domain-event collection and persistence (ADR-009): auditing
  must not be implemented independently by each module.
- Multi-tenancy must remain possible without a later schema migration.

---

## Decision

Auditing and soft delete are **persistence concerns**, not domain contracts. They are applied
transparently by the persistence layer using EF Core **shadow properties** and a centralized
interceptor. Domain entities do **not** implement auditing interfaces and do **not** reference
`BSX.BuildingBlocks` for auditing.

### 1. Ownership

- `IAuditableEntity` and `ISoftDeletable` are **removed from the platform contracts**. No domain
  entity implements them.
- The mechanism lives in module infrastructure as an EF Core `SaveChangesInterceptor`
  (`AuditingInterceptor`) plus Fluent API configuration. It is configured once per `DbContext`.

### 2. Canonical fields (shadow properties)

| Field | Purpose |
| --- | --- |
| `CreatedOnUtc` | Creation timestamp, UTC. Set once. |
| `CreatedBy` | Actor that created the row. |
| `UpdatedOnUtc` | Last modification timestamp, UTC. |
| `UpdatedBy` | Actor that last modified the row. |
| `DeletedOnUtc` | Soft-delete timestamp, UTC. |
| `DeletedBy` | Actor that soft-deleted the row. |
| `IsDeleted` | Soft-delete flag. |
| `TenantId` | Reserved for multi-tenancy; populated only after the tenancy phase. |

Shadow properties are invisible to the domain model. A module that needs a business-visible
lifecycle value (for example, a customer's `RegisteredOn` used in rules) models it explicitly as
domain state; it is never the same field as the technical audit column.

### 3. Timestamps

- All timestamps come from `TimeProvider` (registered in DI), never `DateTimeOffset.UtcNow`.
- Domain events that carry `OccurredOnUtc` also resolve through `TimeProvider` once the kernel
  change in ADR-009 is applied.

### 4. Actor

- The actor is resolved from `ICurrentUser` (ADR-012).
- When there is no authenticated user, the actor is the reserved system value `system`. The
  policy is fixed platform-wide so audit data is never ambiguous.

### 5. Soft delete

- Soft delete is the default for business data. The interceptor converts a `Deleted` state into
  `IsDeleted = true` plus `DeletedOnUtc`/`DeletedBy` and never physically removes the row.
- A global query filter excludes soft-deleted rows by default. Access to deleted rows requires an
  explicit, justified use of `IgnoreQueryFilters`.
- Hard delete is permitted only for non-business/infrastructure data (for example, expired
  outbox rows) and must be explicit.
- Uniqueness constraints on soft-deletable data use PostgreSQL **partial unique indexes**
  (unique where `IsDeleted = false`) so a deleted record does not block re-creation.

### 6. Save pipeline (single ordered interceptor)

Auditing is one stage of the shared save pipeline defined in ADR-009. Order is fixed:

```text
1. Stamp auditing (create/update)
2. Apply soft delete
3. Apply tenant (reserved)
4. Collect domain events from tracked aggregates
5. Translate + write outbox rows
6. SaveChanges (single transaction)
```

No module re-implements any of these stages.

### 7. Configuration conventions

- No data annotations. Auditing and soft delete are configured through the interceptor and
  centralized Fluent API configuration.
- Every aggregate has its own entity configuration (database-skill), which may opt out of soft
  delete explicitly and with justification.

---

## Alternatives Considered

### Keep `IAuditableEntity` / `ISoftDeletable` in BuildingBlocks

Pros

- Explicit contract; domain can read the fields.

Cons

- Domain → BuildingBlocks dependency; violates Rule 1 and Clean Architecture per module.
- Leaks persistence fields into the domain model.

### Move the interfaces to SharedKernel

Pros

- Domain can implement them without referencing BuildingBlocks.

Cons

- Rule 9 restricts the kernel to domain primitives; audit fields are a persistence concern.
- Grows the kernel with technical, EF-oriented members.

### Auditing fields on a domain base entity

Pros

- Simple.

Cons

- Couples the domain to auditing and to EF materialization.
- Makes the fields mutable by domain code, defeating the purpose.

### Shadow properties with a centralized interceptor (chosen)

Pros

- Domain stays clean; no upward dependency.
- Automatic, consistent, testable, multi-tenant ready.

Cons

- Audit fields are not visible to the domain (accepted; use explicit domain state when needed).

---

## Consequences

### Positive

- The Domain → BuildingBlocks violation is eliminated.
- Auditing is automatic and consistent across modules.
- Deterministic timestamps through `TimeProvider`.
- Soft delete is uniform and index-safe.
- Multi-tenancy and the outbox share one transactional pipeline.

### Negative

- Audit fields require raw SQL or shadow-property queries for reporting.
- The interceptor is shared infrastructure and must be maintained centrally.

---

## Notes

- The interfaces are removed because no module implemented them yet; the change is safe.
- Referenced by: [ADR-009](ADR-009-Domain-Event-Dispatching-Strategy.md),
  [ADR-012](ADR-012-Authorization-Architecture.md),
  [auditing.md](../diagrams/auditing.md),
  [implementation-guidelines.md](../architecture/implementation-guidelines.md).
