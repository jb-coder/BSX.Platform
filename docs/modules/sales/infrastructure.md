# Sales - Infrastructure

Status: Infrastructure implemented. API and UI are out of scope for this slice.

## Project and dependencies

`src/Modules/Sales/BSX.Modules.Sales.Infrastructure`:

| Reference | Purpose |
| --- | --- |
| `BSX.SharedKernel` | Primitives/results used by the domain. |
| `BSX.BuildingBlocks` | Persistence seams, authorization context, transaction/event abstractions. |
| `BSX.Modules.Sales.Domain` | Aggregates and value objects. |
| `BSX.Modules.Sales.Application` | Read-model ports and DTOs. |

It has no reference to CRM Domain, CRM Infrastructure or any other module.

## Registration

`AddSalesInfrastructure(connectionString)` configures:

- `SalesDbContext` with Npgsql, `sales` schema default and
  `sales.__ef_migrations_history`.
- `SalesTenantScope` / `ISalesTenantScope` from `ITenantContext`.
- `SalesAuditingSaveChangesInterceptor` (auditing, soft delete, search columns, tenant key,
  concurrency).
- `ICrmReferenceValidator` + `CrmReferenceValidationInterceptor`.
- `LeadRepository`, `OpportunityRepository`.
- `LeadReadRepository`, `OpportunityReadRepository`.
- `IModuleUnitOfWork` → `SalesUnitOfWork` via `TryAddEnumerable` (never `IUnitOfWork`).

`AddSalesApplication()` registers handlers, validators and `SalesTenantResolver`; the host calls
`AddCompositeUnitOfWork()` after all module infrastructure registrations.

## Repositories

| Repository | Responsibility |
| --- | --- |
| `LeadRepository` | Tenant-scoped aggregate load and add. |
| `OpportunityRepository` | Tenant-scoped aggregate load, add, and `ExistsByLeadIdAsync`. |
| `LeadReadRepository` | DTO-only detail/list projections with title search. |
| `OpportunityReadRepository` | DTO-only detail/list projections with title/customer-id search and weighted amount. |

List queries never load aggregates. Detail/list reads use `AsNoTracking`. `ExistsByLeadIdAsync` is
used by `CreateOpportunityFromLeadCommandHandler` as a pre-check; the unique index is the race-safe
guarantee.

## CRM reference validation strategy

### The seam

`BSX.Modules.Sales.Infrastructure.Crm.ICrmReferenceValidator`:

```text
CustomerExistsAsync(TenantId tenantId, CustomerId customerId, CancellationToken)
OrganizationExistsAsync(TenantId tenantId, OrganizationId organizationId, CancellationToken)
```

It is tenant-aware and uses Sales-local value objects. Implementations must use CRM
contracts/interfaces only; they must not reference CRM Domain or query CRM tables directly.

### Enforcement

`CrmReferenceValidationInterceptor` runs during `SaveChanges` for added or modified `Lead` and
`Opportunity` entities. It validates every non-null customer/organization reference and throws
`CrmReferenceValidationException` with the reference type, id and tenant when validation fails.
The future API maps this exception to a controlled client error (for example `400`/`409`) rather
than an unhandled `500`.

### Current state and future integration

The default registration is `PassThroughCrmReferenceValidator`, which accepts all references. This
is deliberate: CRM currently has no `Contracts` project, and Sales must not couple to the CRM
Domain or schema. The seam exists, is wired, and is covered by tests with a rejecting fake.

Before Sales writes are exposed in production:

1. Introduce the CRM contract/interface for customer and organization reference lookup.
2. Implement `ICrmReferenceValidator` in Sales Infrastructure using only that contract.
3. Replace `PassThroughCrmReferenceValidator` in composition (the interceptor requires no change).
4. Add integration tests against the CRM contract implementation.

This keeps the dependency direction correct and prepares for future `CRM.Contracts` integration.

## Tenant isolation

The same two-layer model as CRM:

1. Repositories take an explicit tenant; reads are tenant-scoped by method contract.
2. `SalesDbContext` global filters are fail-closed:
   `!IsDeleted && (IsSystemScope || (CurrentTenantId != null && TenantKey == CurrentTenantId))`.
   A missing tenant sees no data.

`SalesTenantScope.UseTenant` and `UseSystemScope` are operational escape hatches for trusted
background code; request paths rely on `ITenantContext`.

## Auditing, soft delete and concurrency

`SalesAuditingSaveChangesInterceptor` follows ADR-010 and the CRM implementation:

- Shadow `CreatedOnUtc`/`CreatedBy`, `UpdatedOnUtc`/`UpdatedBy`, `DeletedOnUtc`/`DeletedBy`.
- Hard deletes become `IsDeleted = true`; owned values (for example `Money`) remain unchanged on
  the row rather than being deleted with the aggregate.
- `Version` is the optimistic concurrency token; `SalesUnitOfWork` converts EF concurrency
  failures into `ConcurrencyConflictException`.

## Migrations

Initial migration: `20261022113527_InitialSales`.

```bash
dotnet tool restore
dotnet ef database update \
  --project src/Modules/Sales/BSX.Modules.Sales.Infrastructure \
  -- --connection "Host=...;Database=bsx_sales;Username=...;Password=..."
```

Migrations are applied out of band; Sales has no startup-migration hook. The design-time factory
reads `--connection` or `ConnectionStrings__Sales`.

## Testing

`tests/Modules/Sales/BSX.Modules.Sales.Infrastructure.IntegrationTests` covers repositories, read
models, tenant isolation, unique conversion, CRM reference validation, search, soft delete,
auditing, concurrency, event dispatch and DI composition. A Testcontainers suite validates the
PostgreSQL provider (migrations, constraints, indexes, search, soft delete, concurrency) and is
skipped when Docker is unavailable.

## Next

- API endpoints and UI.
- CRM contract-backed `ICrmReferenceValidator`.
- Sales database/schema readiness health checks.
- Additional list filters (status, stage, customer) and reporting projections.
