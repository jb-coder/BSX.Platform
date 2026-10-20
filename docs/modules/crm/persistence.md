# CRM - Persistence and Infrastructure

Status: Infrastructure implemented (v0.3.0 development). API and UI are out of scope.

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
2. **Global tenant query filters.** `CrmDbContext` reads `ITenantContext.TenantId` and applies:
   `!IsDeleted && (CurrentTenantId == null || TenantKey == CurrentTenantId)`.
   `TenantKey` is a mapped `uuid` populated from the aggregate's `TenantId`, so the filter is
   provider-translatable (SQLite tests and PostgreSQL).

Behavior when no ambient tenant is present: the tenant clause is open (background/ops queries see
all tenants). **Request paths must always resolve a tenant** (the Application `CrmTenantResolver`
does, and every repository requires one), so sanctioned paths remain tenant-scoped. Set
`ITenantContext` in every scoped host/service that queries CRM data directly.

Unique index: `(TenantId, Code)` with filter `"IsDeleted" = false`. The same code is allowed in
different tenants and can be reused after a soft delete.

## Search semantics

Search is case-insensitive and partial, and is implemented with lower-cased denormalized columns
(`LIKE`), so it behaves the same on SQLite (tests) and PostgreSQL (production).

| Read model | Matches | Indexes |
| --- | --- | --- |
| Customers | `CustomerCode` **or** `CustomerName` | `(TenantId, SearchCode)`, `(TenantId, SearchName)` |
| Organizations | `OrganizationName` **or** `TaxIdentifier` | `(TenantId, SearchName)`, `(TenantId, SearchTaxIdentifier)` |

Empty/whitespace search returns all rows (subject to tenant and soft-delete filters).

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

> **Composition note (host responsibility).** Each module registers its own `IUnitOfWork` with
> `TryAddScoped`. When the host loads both Identity and CRM, it must compose a module-aware unit of
> work (or coordinate the module units) so a command persists the correct module context and
> dispatches each event once. This is an API/host integration task, not a CRM Infrastructure defect.

## Concurrency

Each aggregate has a shadow `Version` column marked as a concurrency token. The interceptor sets it
to 1 on insert and increments it on update; EF includes the original value in `UPDATE ... WHERE`, so
a stale write affects zero rows. The unit of work converts this into
`ConcurrencyConflictException` for the caller to surface as a conflict.

## Migrations

- Initial migration: `InitialCrm` (`Persistence/Migrations`).
- Apply out of band per [database-migrations.md](../../operations/database-migrations.md); do not
  enable `ApplyMigrationsOnStartup` in multi-instance deployments.
- Design-time factory: `CrmDbContextFactory` (Npgsql).

## Testing

`tests/Modules/Crm/BSX.Modules.Crm.Infrastructure.IntegrationTests` runs against SQLite in-memory
(provider-compatible) and covers repository round trips, tenant isolation and tenant query filters,
customer-code uniqueness, search, soft delete, auditing, optimistic concurrency, domain-event
dispatch and read-model projections.
