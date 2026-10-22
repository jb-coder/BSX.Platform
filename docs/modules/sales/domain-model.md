# Sales - Domain Model

Status: Domain implemented. Application, Infrastructure, API and UI are out of scope for this
slice.

## Scope

The Sales domain currently models **lead management** and **opportunity management** only:

- `Lead` - an unqualified sales prospect.
- `Opportunity` - a qualified commercial opportunity created from a qualified lead.

Quotes, orders, invoices, pricing, forecasting engines, reporting, infrastructure, API and UI are
explicitly out of scope.

The domain is persistence-independent: it references `BSX.SharedKernel` only (no EF Core,
BuildingBlocks, CRM or any other module).

---

## Aggregates

### Lead (`AggregateRoot<LeadId>`)

The consistency boundary for a sales prospect.

| Member | Meaning |
| --- | --- |
| `TenantId` | Tenant identifier (multi-tenancy, ADR-013). |
| `Title` | Lead title (`LeadTitle`). |
| `Status` | `New`, `Contacted`, `Qualified` or `Disqualified`. |
| `CustomerId?` | Optional CRM customer integration reference (identifier only). |
| `OrganizationId?` | Optional CRM organization integration reference (identifier only). |
| `Notes` | Optional bounded free-text notes (max 2,000 characters). |

Behavior: `Create`, `UpdateDetails`, `MarkContacted`, `Qualify`, `Disqualify`, `ChangeNotes`.

Lifecycle:

| From | Operation | To | Rule |
| --- | --- | --- | --- |
| - | `Create` | `New` | Raises `LeadCreated`. |
| `New` | `MarkContacted` | `Contacted` | No event. |
| `Contacted` | `Qualify` | `Qualified` | Raises `LeadQualified`; requires prior contact. |
| `New`, `Contacted` | `Disqualify` | `Disqualified` | Raises `LeadDisqualified`. |
| `Qualified`, `Disqualified` | - | - | Terminal; only `ChangeNotes` is allowed. |

Invariants:

- A new lead is `New`.
- `MarkContacted` is only valid from `New`; contacting a contacted lead is a conflict.
- `Qualify` is only valid from `Contacted`; it cannot happen twice.
- `Disqualify` is valid from `New` or `Contacted`; a qualified lead cannot be disqualified.
- `UpdateDetails` is only valid while the lead is `New` or `Contacted`.
- `ChangeNotes` is valid in **every** status, including `Qualified` and `Disqualified`.
- Notes are trimmed; whitespace becomes `null`; the maximum length is 2,000.

Domain events: `LeadCreated`, `LeadQualified`, `LeadDisqualified`.

### Opportunity (`AggregateRoot<OpportunityId>`)

A qualified commercial opportunity. It always originates from a qualified lead.

| Member | Meaning |
| --- | --- |
| `TenantId` | Tenant identifier; must match the source lead. |
| `LeadId` | Source lead identifier. |
| `Title` | Opportunity title (`OpportunityTitle`). |
| `CustomerId?` | CRM customer reference copied from the lead. |
| `OrganizationId?` | CRM organization reference copied from the lead. |
| `Amount` | Potential revenue (`Money`). |
| `Probability` | Probability of winning (`Probability`). |
| `ExpectedCloseDate` | Expected close date (`ExpectedCloseDate`). |
| `Stage` | `Discovery`, `Qualification`, `Proposal`, `Negotiation`, `Won` or `Lost`. |
| `WeightedAmount` | Computed revenue forecast: `Amount` weighted by `Probability`. |
| `IsTerminal` | `true` for `Won` and `Lost`. |

Behavior: `CreateFromLead`, `UpdateDetails`, `UpdateForecast`, `AdvanceStage`, `MarkWon`,
`MarkLost`.

Stage transitions:

| From | Operation | To |
| --- | --- | --- |
| - | `CreateFromLead` | `Discovery` |
| `Discovery` | `AdvanceStage` | `Qualification` |
| `Qualification` | `AdvanceStage` | `Proposal` |
| `Proposal` | `AdvanceStage` | `Negotiation` |
| `Negotiation` | `MarkWon` | `Won` |
| Any open stage | `MarkLost` | `Lost` |
| `Won`, `Lost` | any mutation | rejected (`OpportunityTerminal`) |

Invariants:

- An opportunity can only be created from a **qualified** lead in the same tenant.
- `AdvanceStage` moves one stage at a time and raises `OpportunityStageChanged`.
- `Negotiation` cannot be advanced; it must be marked won or lost.
- `MarkWon` is only valid from `Negotiation`; it sets the probability to 100%.
- `MarkLost` is valid from any open stage; it sets the probability to 0%.
- `Won` and `Lost` are terminal: details, forecasts and stages can no longer change.
- `UpdateDetails` and `UpdateForecast` are only valid while the opportunity is open.
- `WeightedAmount` is the revenue forecast, rounded to two decimals (away from zero).

Domain events: `OpportunityCreated`, `OpportunityStageChanged`, `OpportunityWon`,
`OpportunityLost`.

---

## CRM integration model

Sales never references CRM aggregates. It carries module-local integration identifiers:

- `CustomerId` - the CRM customer identifier.
- `OrganizationId` - the CRM organization identifier.

The identifiers are value objects owned by Sales (`BSX.Modules.Sales.Domain.ValueObjects`) and are
mapped to the CRM identifiers by the Application layer. There is no cross-module navigation, no
shared domain type and no database dependency.

`Opportunity.CreateFromLead` copies the lead's optional customer and organization references. A
lead or opportunity may therefore be unlinked from CRM until the Application layer associates it.

---

## Value objects

| Value object | Notes |
| --- | --- |
| `LeadId`, `OpportunityId` | Strongly typed identifiers (shared `SalesId` base). |
| `CustomerId`, `OrganizationId` | CRM integration references (identifier only). |
| `TenantId` | Tenant identifier for multi-tenancy. |
| `LeadTitle`, `OpportunityTitle` | Trimmed, non-empty, max 200. Distinct types to prevent accidental mixing. |
| `Money` | Non-negative amount, max two decimals, ISO 4217 currency; `ApplyPercentage` supports revenue weighting. |
| `Probability` | Whole percentage `0..100`; exposes `AsFraction()` and `Certain`/`None`. |
| `ExpectedCloseDate` | `DateOnly`; today or later, at most 10 years ahead; validated against a supplied `today`. |

## Entities

There are no owned child entities in this slice; both aggregates are single-entity consistency
boundaries.

## Repositories

Aggregate-specific, tenant-scoped reads:

- `ILeadRepository`: `GetByIdAsync(TenantId, LeadId)`, `AddAsync`.
- `IOpportunityRepository`: `GetByIdAsync(TenantId, OpportunityId)`, `AddAsync`.

## Errors

`SalesErrors` is the stable `SALES.*` catalog. Value-object failures use `Error.Validation`;
lifecycle and state conflicts use `Error.Conflict`.

## Testing

`tests/Modules/Sales/BSX.Modules.Sales.Domain.UnitTests` covers:

- Lead lifecycle and terminal-state rules.
- Lead detail editing and notes behavior.
- Qualification and disqualification rules and domain events.
- Opportunity creation, tenant matching and integration-reference copying.
- All valid and invalid stage transitions.
- Terminal `Won`/`Lost` behavior.
- Revenue weighting, rounding and probability behavior.
- Value-object validation (titles, money, probability, expected close date, identifiers).
- Domain isolation and tenant-scoped repository contracts.

## Out of scope / next

- Application commands, queries and FluentValidation.
- Infrastructure (EF Core configuration, migrations, tenant query filters).
- API endpoints and UI.
- Quotes, orders, invoices, pricing, forecasting engines and reporting.
- Enforcing a single opportunity per qualified lead (an Application/Infrastructure concern for a
  later slice).
