# Auditing

Automatic auditing and soft delete applied by the shared persistence pipeline (ADR-010).

## Save Pipeline

```mermaid
sequenceDiagram
    autonumber
    participant H as Command Handler
    participant UoW as Unit of Work
    participant DB as DbContext
    participant IC as AuditingInterceptor
    participant TP as TimeProvider
    participant CU as ICurrentUser
    participant EV as Domain Event Collector
    participant OB as Outbox
    participant PG as PostgreSQL

    H->>UoW: SaveChangesAndDispatchAsync()
    UoW->>DB: SaveChanges()
    DB->>IC: SavingChanges
    IC->>TP: GetUtcNow()
    IC->>CU: resolve actor
    IC->>IC: stamp CreatedOnUtc / CreatedBy (insert)
    IC->>IC: stamp UpdatedOnUtc / UpdatedBy (update)
    IC->>IC: apply soft delete (Deleted state)
    IC->>IC: apply reserved TenantId
    IC->>EV: collect domain events
    EV->>OB: write outbox rows
    DB->>PG: COMMIT (state + audit + events)
```

## Shadow Properties

```mermaid
flowchart LR
    subgraph Domain
        AGG["Aggregate<br/>(no audit members)"]
    end
    subgraph Persistence
        CFG["Fluent configuration<br/>+ interceptor"]
        SH["Shadow properties<br/>CreatedOnUtc, CreatedBy,<br/>UpdatedOnUtc, UpdatedBy,<br/>DeletedOnUtc, DeletedBy,<br/>IsDeleted, TenantId"]
        FILTER["Global query filter<br/>IsDeleted = false"]
    end
    AGG -. mapped by .-> CFG
    CFG --> SH
    SH --> FILTER
```

## Soft Delete

```mermaid
flowchart TD
    DEL[Delete requested] --> INT[Interceptor]
    INT -->|business data| SOFT["IsDeleted = true<br/>+ DeletedOnUtc / DeletedBy"]
    INT -->|infrastructure data| HARD["Physical delete<br/>(explicit only)"]
    SOFT --> Q{Query}
    Q -->|default| EX[Excludes deleted rows]
    Q -->|IgnoreQueryFilters| INC[Includes deleted rows]
```

- Uniqueness on soft-deletable data uses partial indexes (`UNIQUE ... WHERE IsDeleted = false`)
  so deleted rows do not block re-creation.
- Actor falls back to the reserved value `system` when there is no authenticated user.
