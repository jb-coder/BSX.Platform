# Sales - Application Layer

Status: Application implemented. Infrastructure, API and UI are out of scope for this slice.

## Scope

The Sales Application layer implements the Lead and Opportunity use cases over the Sales domain:

- Lead commands and queries.
- Opportunity commands and queries.
- Tenant resolution, validation, authorization and DTO projections.

It orchestrates only. Business rules remain in the domain aggregates; the application translates
requests, resolves the tenant, loads aggregates or read projections, invokes domain behavior and
persists through the unit of work.

## Commands

### Leads

| Command | Permission | Result |
| --- | --- | --- |
| `CreateLead` | `sales.leads.create` | New lead id |
| `UpdateLead` | `sales.leads.update` | `204`-style success |
| `MarkLeadContacted` | `sales.leads.update` | success |
| `QualifyLead` | `sales.leads.update` | success |
| `DisqualifyLead` | `sales.leads.update` | success |

`UpdateLead` updates details (title, customer reference, organization reference) while the lead is
`New` or `Contacted`. On a terminal (`Qualified` or `Disqualified`) lead it accepts notes-only
updates; the request must repeat the current title and references, and changing any detail returns
`SALES.Lead.NotEditable`.

### Opportunities

| Command | Permission | Result |
| --- | --- | --- |
| `CreateOpportunityFromLead` | `sales.opportunities.create` | New opportunity id |
| `UpdateOpportunity` | `sales.opportunities.update` | success |
| `AdvanceOpportunityStage` | `sales.opportunities.update` | success |
| `MarkOpportunityWon` | `sales.opportunities.update` | success |
| `MarkOpportunityLost` | `sales.opportunities.update` | success |

## Queries

| Query | Permission | Result |
| --- | --- | --- |
| `GetLeadById` | `sales.leads.read` | `LeadDto` |
| `ListLeads` | `sales.leads.read` | `PagedResult<LeadSummaryDto>` |
| `GetOpportunityById` | `sales.opportunities.read` | `OpportunityDto` |
| `ListOpportunities` | `sales.opportunities.read` | `PagedResult<OpportunitySummaryDto>` |

## Adopted domain decisions

### 1. One qualified lead produces one opportunity

- `CreateOpportunityFromLeadCommandHandler` checks
  `IOpportunityRepository.ExistsByLeadIdAsync(tenant, leadId)` before creating the opportunity and
  returns `SALES.Opportunity.AlreadyExistsForLead` on a duplicate.
- **Infrastructure enforcement (required):** add a unique index on `(TenantId, LeadId)` (filtered
  by `IsDeleted = false` once opportunities are soft-deletable) and implement
  `ExistsByLeadIdAsync` tenant-scoped. The handler check is the friendly pre-check; the unique index
  closes the race between concurrent conversions.

### 2. CustomerId is required when creating an opportunity

- `CreateOpportunityFromLeadCommand` requires a non-empty `CustomerId`.
- `Opportunity.CreateFromLead` requires a `CustomerId` and persists it as a required reference.
- `UpdateOpportunityCommand` also requires a non-empty `CustomerId`, so the invariant cannot be
  cleared through updates.
- **Infrastructure enforcement (required):** persist `CustomerId` as non-nullable/required and
  validate that the referenced CRM customer exists in the tenant through CRM contracts (not by
  referencing CRM domain/infrastructure). This validation belongs to the future CRM integration
  path; it must not couple Sales to CRM aggregates.

### 3. Qualification requires the lead to be Contacted first

- The domain enforces `New → Contacted → Qualified`. The Application calls `Lead.Qualify()` and
  propagates `SALES.Lead.MustBeContacted`; no duplicate application validation exists.

## Multi-tenancy

- Tenants come exclusively from `ITenantContext` through `SalesTenantResolver`.
- No command or query accepts `TenantId`.
- Every aggregate read and read-model query is tenant-scoped (`GetByIdAsync(TenantId, ...)` or
  `GetByIdAsync(Guid tenantId, ...)`).
- A request without a tenant fails closed with `SALES.Tenant.Required`.

## Validation

FluentValidation validators live beside each request. They validate request shape only: required
fields, identifier non-empty checks, title length and currency shape. Domain rules — probability
range, money amount, expected-close-date horizon and lifecycle transitions — are **not** duplicated;
the handlers create the domain value objects and return their `Result` errors.

## Read models

- `ILeadReadRepository` and `IOpportunityReadRepository` are Application-owned ports implemented by
  Infrastructure.
- Detail and list reads project directly to DTOs; list queries never load aggregates.
- Leads are searchable by title; opportunities are searchable by title.
- Paging is normalized in the query handlers (page ≥ 1; size 1–100, default 20).
- `OpportunityDto`/`OpportunitySummaryDto` expose `WeightedAmount`; Infrastructure should compute it
  with the same two-decimal away-from-zero rounding as `Money.ApplyPercentage`.

## Permissions

```text
sales.leads.read
sales.leads.create
sales.leads.update

sales.opportunities.read
sales.opportunities.create
sales.opportunities.update
```

Every command/query declares exactly one `[RequirePermission]`; the application tests lock this.

## Errors

`SalesApplicationErrors` adds the application-level codes:

- `SALES.Tenant.Required`
- `SALES.Lead.NotFound`, `SALES.Lead.InvalidId`
- `SALES.Opportunity.NotFound`, `SALES.Opportunity.InvalidId`,
  `SALES.Opportunity.AlreadyExistsForLead`
- `SALES.Customer.InvalidId`, `SALES.Organization.InvalidId`

Domain failures (invalid titles/money/probability/date, lifecycle conflicts) propagate from
`SalesErrors` unchanged.

## Composition

`AddSalesApplication()` registers the Sales handlers, validators and `SalesTenantResolver` through
`AddBuildingBlocks`. The host composes it with the Sales Infrastructure and the platform unit of
work when the Infrastructure slice is available.

## Testing

`tests/Modules/Sales/BSX.Modules.Sales.Application.UnitTests` covers:

- Every command handler and query handler.
- Tenant resolution and missing-tenant failure.
- Permission attributes for every request.
- Repository interactions (tenant scoping, duplicate conversion lookup, saves).
- Validators (shape only, no duplicated domain rules).
- Domain failure propagation.
- Notes-only updates on terminal leads.
- Required `CustomerId` and one-opportunity-per-lead enforcement.
- Read-model paging and DTO projection usage.

## Out of scope

- Infrastructure (EF Core, migrations, unique indexes, CRM reference validation).
- API endpoints and UI.
- Quotes, orders, invoices, pricing, forecasting engines and reporting.
