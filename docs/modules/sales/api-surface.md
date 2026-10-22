# Sales - API Surface

Status: Domain, Application, Infrastructure and HTTP API implemented. Quotes, orders, invoices,
pricing, forecasting, reporting and UI are out of scope.

## Conventions

- Base path: `/sales`.
- **Authentication is required by default** for every endpoint (JWT bearer, ADR-017).
- **Authorization is permission-based** (`[RequirePermission]` on the Application requests) and
  fail-closed; a missing permission returns `403`.
- **Tenant-aware**: the tenant is resolved from the authenticated token (`ITenantContext`), never
  from client input. Cross-tenant resources are not visible (`404`).
- Result mapping follows ADR-011 (`application/problem+json`); the stable `SALES.*` code is the
  problem title.
- CRM customer/organization references are validated through `CRM.Contracts` before persistence
  (see [CRM reference validation](#crm-reference-validation)).
- Commands persist through the module-aware `CompositeUnitOfWork`; domain events dispatch once.

## Leads

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| GET | `/sales/leads?page=&size=&search=` | `sales.leads.read` | `200` `PagedResult<LeadSummaryDto>` |
| GET | `/sales/leads/{id}` | `sales.leads.read` | `200` `LeadDto` |
| POST | `/sales/leads` | `sales.leads.create` | `201` + `Location`, body = id |
| PUT | `/sales/leads/{id}` | `sales.leads.update` | `204` |
| POST | `/sales/leads/{id}/contacted` | `sales.leads.update` | `204` |
| POST | `/sales/leads/{id}/qualify` | `sales.leads.update` | `204` |
| POST | `/sales/leads/{id}/disqualify` | `sales.leads.update` | `204` |

- Create body:
  `{ "title", "customerId", "organizationId", "notes" }` (`customerId`, `organizationId` and
  `notes` are optional).
- Update body: the same shape. Details are editable only while the lead is `New` or `Contacted`.
  On a terminal lead (`Qualified` or `Disqualified`) the request must repeat the current title and
  references; only `notes` may change, otherwise the response is `409`
  `SALES.Lead.NotEditable`.
- Lifecycle: `New → Contacted → Qualified`, or `Disqualified` from `New`/`Contacted`. Invalid
  transitions return `409` (for example `SALES.Lead.MustBeContacted`).
- `search` is a case-insensitive partial match on the title.

## Opportunities

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| GET | `/sales/opportunities?page=&size=&search=` | `sales.opportunities.read` | `200` `PagedResult<OpportunitySummaryDto>` |
| GET | `/sales/opportunities/{id}` | `sales.opportunities.read` | `200` `OpportunityDto` |
| POST | `/sales/opportunities` | `sales.opportunities.create` | `201` + `Location`, body = id |
| PUT | `/sales/opportunities/{id}` | `sales.opportunities.update` | `204` |
| POST | `/sales/opportunities/{id}/advance` | `sales.opportunities.update` | `204` |
| POST | `/sales/opportunities/{id}/won` | `sales.opportunities.update` | `204` |
| POST | `/sales/opportunities/{id}/lost` | `sales.opportunities.update` | `204` |

- Create body:
  `{ "leadId", "customerId", "title", "amount", "currency", "probability", "expectedCloseDate" }`.
  `customerId` is required. The lead must already be `Qualified` and may produce only one
  opportunity (`409 SALES.Opportunity.AlreadyExistsForLead` on a duplicate conversion).
- Update body:
  `{ "customerId", "organizationId", "title", "amount", "currency", "probability", "expectedCloseDate" }`.
  Details and forecast can change only while the opportunity is open; terminal opportunities return
  `409 SALES.Opportunity.Terminal`.
- Stage flow: `Discovery → Qualification → Proposal → Negotiation → Won`; `Lost` is reachable from
  any open stage. `advance` moves exactly one stage. `won` is valid only from `Negotiation` and sets
  probability to 100%; `lost` sets probability to 0%.
- `search` matches the title, or the customer id when the term is a GUID.
- `OpportunityDto`/`OpportunitySummaryDto.WeightedAmount` is the revenue forecast (amount ×
  probability, rounded to two decimals away from zero).

## Status codes

| Result | HTTP |
| --- | --- |
| Success (no body) | `204` |
| Success (read) | `200` |
| Created | `201` + `Location` |
| Validation (shape, invalid id, invalid currency) | `400` |
| Unauthenticated | `401` |
| Missing permission / missing tenant | `403` |
| Not found / cross-tenant resource | `404` |
| Domain conflict (invalid transition, duplicate conversion, terminal state) | `409` |
| Invalid CRM customer/organization reference | `409` |
| Concurrency conflict (stale aggregate version) | `409` `SALES.ConcurrencyConflict` |
| Unavailable | `503` |

Errors are RFC 9457 problem details. Concurrency conflicts are raised by the unit of work as
`ConcurrencyConflictException` and mapped by the Sales endpoint filter; the stale write is not
persisted.

## CRM reference validation

Sales must not reference CRM Domain, Application or Infrastructure directly. The integration
boundary is:

```text
Sales.Infrastructure.ICrmReferenceValidator
        ↓ (Sales-local value objects)
Sales.Infrastructure.CrmContractReferenceValidator
        ↓ (CRM.Contracts)
CRM.Contracts.References.ICrmReferenceLookup
        ↓ (implemented by CRM.Infrastructure)
CRM.Infrastructure.References.CrmReferenceLookup
        ↓
CrmDbContext (crm schema)
```

- `CrmReferenceLookup` is tenant-aware and excludes soft-deleted rows.
- `AddSalesInfrastructure` registers a **fail-closed** fallback lookup. If the CRM module is not
  composed, every reference is rejected and Sales cannot persist unverifiable data.
- `CrmReferenceValidationInterceptor` validates added/modified leads and opportunities during
  `SaveChanges`. `SalesCrmReferenceExceptionFilter` maps the resulting
  `CrmReferenceValidationException` to `409` with `SALES.Customer.NotFound` or
  `SALES.Organization.NotFound` (never HTTP 500).
- Composition order: register CRM infrastructure before Sales infrastructure so the CRM contract
  replaces the fail-closed fallback.

## Permissions

```text
sales.leads.read
sales.leads.create
sales.leads.update

sales.opportunities.read
sales.opportunities.create
sales.opportunities.update
```

The codes live in `BSX.Modules.Sales.Contracts.Permissions.SalesPermissionCodes` and are published
through `SalesPermissionCatalog` (`IPermissionCatalogSource`, ADR-018). `AddSalesApplication`
registers the catalog. Identity's `SystemRolePermissionSynchronizer` grants the catalog to the
`Administrator` system role and every `.read` permission to `Auditor`
(`Identity:Permissions:SynchronizeOnStartup`). Role assignments are still managed by Identity;
Sales never grants permissions itself.

## Health

- `AddSalesHealthChecks()` registers `sales-database` with the `ready` tag.
- `AddSalesSchemaHealthCheck()` (enabled by `Sales:Database:VerifySchema=true`) registers
  `sales-database-schema`, which reports `Unhealthy` when Sales migrations are pending.
- `/health/ready` includes the Sales checks alongside Identity and CRM; `/health` aggregates all.

## Integration notes

- JWT authentication, `ICurrentUser` and `ITenantContext` come from the Identity authentication
  pipeline; Sales does not issue tokens.
- Every Sales command runs through the single module-aware `CompositeUnitOfWork`, which persists
  all composed module contexts and dispatches each domain event exactly once.
- Sales exposes no integration events in this slice; cross-module communication uses the published
  CRM contract only.
