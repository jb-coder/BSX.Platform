# CRM - Persistence and Infrastructure

Status: Domain, Application, Infrastructure and HTTP API implemented and release-hardened
(v0.3.0 development). UI, reporting and analytics are out of scope.

## Project

`src/Modules/Crm/BSX.Modules.Crm.Infrastructure` references `BSX.SharedKernel`,
`BSX.BuildingBlocks`, `BSX.Modules.Crm.Domain` and `BSX.Modules.Crm.Application`.
It is PostgreSQL-only (Npgsql) and owns the `crm` schema.

`AddCrmInfrastructure(connectionString)` registers the `CrmDbContext`, repositories, the unit of
work and the auditing interceptor.

## Schema

Default schema: `crm`. Migration history: `crm.__ef_migrations_history`.

| Table | Aggregate / owned type | Notes |
| --- | --- | --- |
| `customers` | `Customer` | Soft delete, auditing, concurrency, tenant filter. |
| `customer_addresses` | `CustomerAddress` (owned many) | Postal `Address` owned columns `line1..country`. |
| `customer_contacts` | `ContactPerson` (owned many) | `Name`, optional `Email`/`PhoneNumber`. |
| `organizations` | `Organization` | Optional owned `Address`; soft delete, auditing, concurrency, tenant filter. |

Aggregates reference each other by identifier only (`OrganizationId` on `Customer`), never by
navigation.

## Mapping conventions

- Strongly typed ids and value objects use EF Core value converters (`CrmValueConverters`):
  `CustomerId`, `OrganizationId`, `CustomerAddressId`, `ContactPersonId`, `TenantId` → `uuid`;
  `CustomerCode`, `CustomerName`, `TaxIdentifier`, `Email`, `PhoneNumber` → strings.
- Enums (`CustomerStatus`, `AddressType`) are stored as strings.
- Owned collections (`Addresses`, `Contacts`) are loaded with the aggregate; the write repository
  explicitly `Include`s them for mutations.
- Auditing, soft-delete and `Version` are shadow properties; no audit fields exist in the domain.

## Auditing (ADR-010)

`CrmAuditingSaveChangesInterceptor` automatically maintains:

- `CreatedOnUtc` / `CreatedBy` on insert,
- `UpdatedOnUtc` / `UpdatedBy` on modify,
- `DeletedOnUtc` / `DeletedBy` + `IsDeleted` on delete (converted to soft delete),
- the denormalized `SearchCode` / `SearchName` / `SearchTaxIdentifier` columns,
- the `TenantKey` column used by the tenant query filter,
- the `Version` concurrency token (set to 1 on insert, incremented on update).

The actor is `ICurrentUser.UserId` (or `"system"` when unauthenticated); time comes from
`TimeProvider`.

## Soft delete

Deletes are converted to updates (`IsDeleted = true`, `DeletedOnUtc`, `DeletedBy`) for `Customer`
and `Organization`. Global query filters exclude soft-deleted rows from all queries. The filtered
unique index still allows code reuse after soft delete.

## Tenant isolation

Two complementary controls:

1. **Explicit tenant on every repository call.** Write repositories take `TenantId` and read
   repositories take a `Guid tenantId`; both filter by tenant. This is the primary guard.
2. **Fail-closed global tenant query filters.** `CrmDbContext` resolves `ICrmTenantScope`
   (registered scoped, derived from `ITenantContext`) and applies:
   `!IsDeleted && (IsSystem || (CurrentTenantId != null && TenantKey == CurrentTenantId))`.
   A **missing tenant returns no data**. `TenantKey` is a mapped `uuid` populated from the
   aggregate's `TenantId`, and a database check constraint (`TenantKey = TenantId`) prevents
   divergence.

**Explicit privileged/system scope.** Background/operations code may widen the scope on the scoped
`CrmTenantScope` instance:

- `UseTenant(tenantId)` for a per-tenant background job.
- `UseSystemScope()` for a privileged cross-tenant job (the soft-delete filter still applies).

Request paths never call these; they rely on the authenticated tenant. A request without a tenant
sees no CRM data.

Unique index: `(TenantId, Code)` with filter `"IsDeleted" = false`. The same code is allowed in
different tenants and can be reused after a soft delete.

## Search semantics

Search is case-insensitive and partial, and is implemented with lower-cased denormalized columns
(`LIKE`), so it behaves the same on SQLite (tests) and PostgreSQL (production).

| Read model | Matches | Indexes |
| --- | --- | --- |
| Customers | `CustomerCode` **or** `CustomerName` | `(TenantId, SearchCode)`, `(TenantId, SearchName)` |
| Organizations | `OrganizationName` **or** `TaxIdentifier` | `(TenantId, SearchName)`, `(TenantId, SearchTaxIdentifier)` |

Empty/whitespace search returns all rows (subject to tenant and soft-delete filters). The pattern
is a leading wildcard (`%term%`), so PostgreSQL cannot use the B-tree indexes for the match itself;
these indexes remain useful for tenant scoping and ordering. For large datasets, add `pg_trgm` GIN
indexes or switch to prefix search (see [crm-operations.md](../../operations/crm-operations.md#search)).

## Read models

`CustomerReadRepository` and `OrganizationReadRepository` project directly to DTOs:

- Detail reads project the required columns plus owned addresses/contacts; customer detail uses
  `AsSplitQuery()` to avoid a cartesian explosion across the two collections.
- List reads project only summary columns and never load aggregates. Paging is normalized
  (page ≥ 1, size 1–100, default 20).
- Organization addresses are single/optional and have no address id; their `AddressDto.Id` is
  `Guid.Empty` and `Type` is `"Other"` (documented mapping detail).

## Unit of work and event dispatch (ADR-009)

`CrmUnitOfWork : UnitOfWorkBase` implements `SaveChangesAsync`, `BeginTransactionAsync`,
`CollectDomainEvents`, `ClearDomainEvents` and `CommitAsync`. `SaveChangesAndDispatchAsync` persists,
commits, clears and dispatches exactly once; a second call dispatches nothing (events already
cleared). `DbUpdateConcurrencyException` is mapped to `ConcurrencyConflictException`.

**Multi-module composition (resolved).** CRM registers its unit of work as `IModuleUnitOfWork`
(never as `IUnitOfWork`). The host calls `AddCompositeUnitOfWork()` after the module infrastructure
registrations. `CompositeUnitOfWork` collects the pending events of every module, saves all contexts,
commits all module transactions, clears the aggregates and dispatches each event exactly once.
Modules remain isolated; commands resolve the composite through `TransactionBehavior`.

## Concurrency

Each aggregate has a shadow `Version` column marked as a concurrency token. The interceptor sets it
to 1 on insert and increments it on update; EF includes the original value in `UPDATE ... WHERE`, so
a stale write affects zero rows. The unit of work converts this into
`ConcurrencyConflictException`.

The CRM endpoint filter (`CrmConcurrencyConflictFilter`) maps that exception to the platform's
RFC 9457 contract: `409 Conflict` with `title = "CRM.ConcurrencyConflict"` and the standard
`application/problem+json` body. The rejected write is not persisted; clients re-read and retry.
This keeps exceptions from leaking as HTTP 500 while preserving the Result/ProblemDetails
convention (ADR-011).

## Migrations

- Initial migration: `InitialCrm` (`Persistence/Migrations`).
- Apply out of band per [database-migrations.md](../../operations/database-migrations.md); there is
  no CRM startup-migration hook.
- Design-time factory: `CrmDbContextFactory` (Npgsql), pinned to
  `crm.__ef_migrations_history`, accepting `--connection` or `ConnectionStrings__Crm`.
- Readiness: `crm-database` (always) and `crm-database-schema` (opt-in via
  `Crm:Database:VerifySchema`).

## Provider parity: SQLite vs PostgreSQL

Integration tests run on SQLite in memory for speed; the production provider is PostgreSQL. The
Testcontainers suite (`PostgreSqlParityTests`) validates the differences that matter:

| Concern | SQLite tests | PostgreSQL parity |
| --- | --- | --- |
| Migrations | `EnsureCreated` (model only) | `database update`/`MigrateAsync` applies `InitialCrm` |
| Schema | No schema (SQLite has none) | `crm` schema + `crm.__ef_migrations_history` |
| Unique index | Relational index enforced | Filtered unique index `(TenantId, Code)` with `"IsDeleted" = false` |
| Check constraints | Not evaluated by SQLite | `CK_*_tenant_key` enforced by PostgreSQL |
| Search | `LIKE` on lower-cased columns | Same semantics (`LIKE` with lower-cased columns) |
| Soft delete | Interceptor + query filter | Same, verified against the real database |
| Concurrency | `Version` token | Same, verified with PostgreSQL row updates |
| `DateTimeOffset`/UUID | SQLite text storage | Native `timestamptz`/`uuid` |

The PostgreSQL tests are skipped when Docker is unavailable and run in CI (GitHub-hosted runners
provide Docker). Set `DOCKER_AVAILABLE=true` to require them.

## Testing

`tests/Modules/Crm/BSX.Modules.Crm.Infrastructure.IntegrationTests` runs against SQLite in-memory
(provider-compatible) and covers repository round trips, tenant isolation, fail-closed tenant query
filters (missing/wrong/correct/system), customer-code uniqueness, search, soft delete, auditing,
optimistic concurrency, domain-event dispatch and read-model projections. The same project contains
`PostgreSqlParityTests` for the production provider.

`tests/Integration/BSX.Web.IntegrationTests` exercises the HTTP surface, including concurrency
conflict mapping (`409 CRM.ConcurrencyConflict`) and CRM schema readiness detection.

`tests/Composition/BSX.Composition.IntegrationTests` proves the module unit-of-work composition:
Identity and CRM commands persist through the same `IUnitOfWork`, each domain event dispatches
exactly once, and the host resolves a single `CompositeUnitOfWork` over both modules.
