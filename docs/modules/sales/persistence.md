# Sales - Persistence and Infrastructure

Status: Infrastructure implemented. API and UI are out of scope for this slice.

## Project

`src/Modules/Sales/BSX.Modules.Sales.Infrastructure` references `BSX.SharedKernel`,
`BSX.BuildingBlocks`, `BSX.Modules.Sales.Domain` and `BSX.Modules.Sales.Application`. It is
PostgreSQL-only (Npgsql) and owns the `sales` schema.

`AddSalesInfrastructure(connectionString)` registers the `SalesDbContext`, repositories, auditing,
CRM reference validation and the module unit of work.

## Schema

Default schema: `sales`. Migration history: `sales.__ef_migrations_history`.

| Table | Aggregate | Notes |
| --- | --- | --- |
| `leads` | `Lead` | Soft delete, auditing, concurrency, tenant filter, title search. |
| `opportunities` | `Opportunity` | Soft delete, auditing, concurrency, tenant filter, title/customer search, unique lead conversion. |

Aggregates reference each other and CRM by identifier only: `Opportunity.LeadId`, `CustomerId` and
`OrganizationId` are columns, not relationships.

## Mapping conventions

- Strongly typed ids and value objects use EF Core value converters (`SalesValueConverters`):
  `LeadId`, `OpportunityId`, `CustomerId`, `OrganizationId`, `TenantId` → `uuid`;
  `LeadTitle`, `OpportunityTitle` → strings; `Probability` → `integer`;
  `ExpectedCloseDate` → `date`.
- `Money` is an owned value object stored in the opportunity row as `Amount`
  (`numeric(18,2)`) and `Currency` (`varchar(3)`).
- Enums (`LeadStatus`, `OpportunityStage`) are stored as strings.
- Auditing, soft-delete, `TenantKey`, `SearchTitle` and `Version` are shadow properties; no audit
  fields exist in the domain.

## Constraints and indexes

- `CK_leads_tenant_key` / `CK_opportunities_tenant_key` enforce `TenantKey = TenantId`.
- `IX_opportunities_TenantId_LeadId` is a **filtered unique index**
  (`"IsDeleted" = false`): one non-deleted opportunity per `(tenant, lead)`; a soft-deleted
  opportunity does not block re-creation.
- `(TenantId, SearchTitle)` supports lead/opportunity title search.
- `(TenantId, CustomerId)` supports opportunity customer filters and search.
- `TenantKey` is indexed for tenant filtering.

## Auditing (ADR-010)

`SalesAuditingSaveChangesInterceptor` maintains:

- `CreatedOnUtc` / `CreatedBy` on insert,
- `UpdatedOnUtc` / `UpdatedBy` on modify,
- `DeletedOnUtc` / `DeletedBy` + `IsDeleted` on delete (converted to soft delete),
- `SearchTitle` from the lower-cased title,
- `TenantKey` from the aggregate `TenantId`,
- the `Version` concurrency token (1 on insert, incremented on update).

The actor is `ICurrentUser.UserId` (or `"system"` when unauthenticated); time comes from
`TimeProvider`.

## Soft delete

Deletes are converted to updates for `Lead` and `Opportunity`. Global query filters exclude
soft-deleted rows from all queries. The filtered unique index still allows a new opportunity for
the same lead after soft delete.

## Tenant isolation

Two complementary controls:

1. **Explicit tenant on every repository call.** Write repositories take `TenantId`; read
   repositories take a `Guid tenantId`. This is the primary guard.
2. **Fail-closed global tenant query filters.** `SalesDbContext` resolves `ISalesTenantScope`
   (registered scoped, derived from `ITenantContext`) and applies:
   `!IsDeleted && (IsSystem || (CurrentTenantId != null && TenantKey == CurrentTenantId))`.
   A missing tenant returns no data. `TenantKey` is constrained to `TenantId` in the database.

**Explicit privileged/system scope.** Background/operations code may widen the scope on the scoped
`SalesTenantScope` instance:

- `UseTenant(tenantId)` for a per-tenant background job.
- `UseSystemScope()` for a privileged cross-tenant job (the soft-delete filter still applies).

Request paths never call these; they rely on the authenticated tenant.

## Concurrency

Each aggregate has a shadow `Version` column marked as a concurrency token. The interceptor sets it
to 1 on insert and increments it on update; EF includes the original value in `UPDATE ... WHERE`, so
a stale write affects zero rows. `SalesUnitOfWork` converts `DbUpdateConcurrencyException` into
`ConcurrencyConflictException`, which the future API maps to `409 Conflict`.

## Search semantics

Search is case-insensitive and partial, implemented with a lower-cased denormalized `SearchTitle`
column (`LIKE`), so it behaves the same on SQLite (tests) and PostgreSQL.

| Read model | Matches | Indexes |
| --- | --- | --- |
| Leads | `Lead.Title` | `(TenantId, SearchTitle)` |
| Opportunities | `Opportunity.Title` **or** `CustomerId` (when the search term parses as a GUID) | `(TenantId, SearchTitle)`, `(TenantId, CustomerId)` |

Empty/whitespace search returns all rows (subject to tenant and soft-delete filters). Search terms
are trimmed and lower-cased; a GUID search term matches the customer reference in addition to the
title.

## Read models

`LeadReadRepository` and `OpportunityReadRepository` project directly to the Application DTOs:

- Detail reads project only the required columns; no aggregate is loaded.
- List reads project summaries and never load aggregates.
- `OpportunityDto`/`OpportunitySummaryDto.WeightedAmount` is computed with the domain
  `Money.ApplyPercentage` rule (two decimals, away from zero).
- Paging is normalized (page ≥ 1, size 1–100, default 20).

## Unit of work and event dispatch (ADR-009)

`SalesUnitOfWork : UnitOfWorkBase` implements `SaveChangesAsync`, `BeginTransactionAsync`,
`CollectDomainEvents`, `ClearDomainEvents` and `CommitAsync`. `SaveChangesAndDispatchAsync` persists,
commits, clears and dispatches exactly once; a second call dispatches nothing.

**Multi-module composition.** Sales registers its unit of work as `IModuleUnitOfWork` (never as
`IUnitOfWork`). The host calls `AddCompositeUnitOfWork()` after module infrastructure
registrations. `CompositeUnitOfWork` collects/saves/commits/clears/dispatches across Identity, CRM
and Sales exactly once. The composition test resolves three modules with one composite unit of work.

## CRM reference validation

Customer and organization references are validated through
`BSX.Modules.Sales.Infrastructure.Crm.ICrmReferenceValidator`, which is tenant-aware and uses
Sales-local identifiers. A `CrmReferenceValidationInterceptor` validates added/modified Lead and
Opportunity references before persistence and throws `CrmReferenceValidationException` when a
reference is rejected.

**Current implementation:** `PassThroughCrmReferenceValidator` accepts every reference. This is an
explicit temporary seam: Sales must not reference CRM Domain or query CRM tables directly. When
`CRM.Contracts` is introduced, replace the registration with a tenant-aware implementation that
checks customer/organization existence through CRM contracts. No Sales domain or application
change is required; the existing interceptor will enforce the replacement automatically.

Tests use a rejecting fake validator to prove customer and organization validation, including
tenant propagation through the seam.

## Migrations

- Initial migration: `InitialSales` (`Persistence/Migrations`).
- Apply out of band:
  ```bash
  dotnet tool restore
  dotnet ef database update \
    --project src/Modules/Sales/BSX.Modules.Sales.Infrastructure \
    -- --connection "Host=...;Database=bsx_sales;Username=...;Password=..."
  ```
- The design-time factory reads `--connection` or `ConnectionStrings__Sales`.
- There is no Sales startup-migration hook; migrations are always applied out of band.

## Testing

`tests/Modules/Sales/BSX.Modules.Sales.Infrastructure.IntegrationTests` covers:

- Repository round trips; read-model projections.
- Tenant isolation and fail-closed/system scopes.
- Unique `(TenantId, LeadId)` and soft-delete reuse.
- CRM reference validation (customer and organization, tenant-aware).
- Search (lead title; opportunity title/customer id).
- Soft delete, auditing and optimistic concurrency.
- Domain-event dispatch exactly once.
- DI composition (`IModuleUnitOfWork`, `CompositeUnitOfWork`).

`PostgreSqlParityTests` validates the production provider with Testcontainers: migrations/schema,
unique index, check constraints, tenant filters, search, soft delete and concurrency. They are
skipped when Docker is unavailable and run in CI (`DOCKER_AVAILABLE=true`).

## Provider parity: SQLite vs PostgreSQL

| Concern | SQLite tests | PostgreSQL parity |
| --- | --- | --- |
| Migrations | `EnsureCreated` (model only) | `MigrateAsync` applies `InitialSales` |
| Schema | No schema (SQLite has none) | `sales` schema + `sales.__ef_migrations_history` |
| Unique index | Relational unique index enforced | Filtered unique index `(TenantId, LeadId)` |
| Check constraints | Evaluated | `CK_*_tenant_key` enforced |
| Search | `LIKE` on lower-cased column | Same semantics |
| Soft delete | Interceptor + query filter | Verified against the real database |
| Concurrency | `Version` token | Verified with PostgreSQL row updates |
| `DateOnly`/`numeric`/`uuid` | Provider-compatible storage | Native `date`/`numeric(18,2)`/`uuid` |

## Out of scope / next

- API endpoints and UI.
- Sales health checks (database/schema readiness).
- A real CRM contracts-backed reference validator.
- DTO-only search filters beyond title/customer id (status, stage, owner) and reporting.
