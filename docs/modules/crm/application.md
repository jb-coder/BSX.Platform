# CRM - Application Layer

Status: Application implemented (v0.3.0 development). Infrastructure, API and UI are out of scope
for this slice.

## Principles

- **CQRS** (ADR-002): commands modify state through aggregates; queries project to DTOs.
- **Result pattern** (ADR-011): every request returns `Result` / `Result<T>`; expected failures are values.
- **Business rules live in the Domain.** Handlers orchestrate: resolve tenant, load aggregates, call
  domain behavior, persist. They do not re-implement invariants.
- **No Infrastructure dependency.** EF Core, `DbContext` and transports are absent.

## Project

`src/Modules/Crm/BSX.Modules.Crm.Application` references `BSX.SharedKernel`,
`BSX.BuildingBlocks` and `BSX.Modules.Crm.Domain` only.

`AddCrmApplication()` registers the CQRS handlers, validators and `CrmTenantResolver` through
`AddBuildingBlocks`.

## Tenant isolation

Every handler resolves the tenant from `ITenantContext` via `CrmTenantResolver`; **no command accepts
a tenant identifier from the caller**. The resolved `TenantId` is passed to every aggregate read and
to the read-model queries (following the Domain decision: tenant in read contracts). A missing tenant
returns `CRM.Tenant.Required` (403).

## Commands

| Area | Command | Permission |
| --- | --- | --- |
| Customers | `CreateCustomer` | `crm.customers.create` |
| | `UpdateCustomer` | `crm.customers.update` |
| | `ActivateCustomer`, `DeactivateCustomer` | `crm.customers.update` |
| | `AssignOrganization`, `ClearOrganization` | `crm.customers.update` |
| Contacts | `AddContact`, `UpdateContact`, `RemoveContact`, `SetPrimaryContact` | `crm.customers.update` |
| Addresses | `AddAddress`, `UpdateAddress`, `RemoveAddress`, `SetDefaultAddress` | `crm.customers.update` |
| Organizations | `CreateOrganization` | `crm.organizations.create` |
| | `UpdateOrganization` | `crm.organizations.update` |

`CreateCustomer` and `CreateOrganization` return the new identifier (`Result<Guid>`). All other
commands return `Result`.

### Application-level orchestration

- **Customer code uniqueness** is checked with `ICustomerRepository.ExistsByCodeAsync(tenant, code)`
  before creation (`CRM.Customer.CodeAlreadyExists`). A database unique index remains the final
  guard in Infrastructure.
- **Organization assignment** loads both the customer and the organization within the tenant, so a
  cross-tenant or missing organization is rejected (`CRM.Organization.NotFound`).
- **Empty identifiers** are rejected defensively (`CRM.*.InvalidId`) before constructing strongly
  typed ids, because `CrmId.From(Guid.Empty)` must not be reached.

## Queries and read model

| Query | Result | Permission |
| --- | --- | --- |
| `GetCustomerById` | `CustomerDto` | `crm.customers.read` |
| `ListCustomers` | `PagedResult<CustomerSummaryDto>` | `crm.customers.read` |
| `GetOrganizationById` | `OrganizationDto` | `crm.organizations.read` |
| `ListOrganizations` | `PagedResult<OrganizationDto>` | `crm.organizations.read` |

Read-side interfaces are separate from the write repositories:

- `ICustomerReadRepository` / `IOrganizationReadRepository` (Application).
- Implementations live in Infrastructure and return DTO projections.
- **List queries never load aggregates**; they project directly and are tenant-scoped.
- Paging is normalized in the handler (page ≥ 1, size 1–100, default 20).

## DTOs

`CustomerDto`, `CustomerSummaryDto`, `OrganizationDto`, `ContactDto`, `AddressDto`, `PagedResult<T>`,
plus `AddressInput` for address writes.

## Validation

FluentValidation validators exist for every command and validate **shape only**: required fields,
maximum lengths, id presence and enum validity. Domain invariants (reachability, uniqueness,
`(address, type)`, active-state rules) are **not** duplicated; the aggregates enforce them.
Queries normalize paging in the handler and do not require validators.

## Testing

`tests/Modules/Crm/BSX.Modules.Crm.Application.UnitTests` covers command handlers, query handlers,
tenant resolution, repository interactions, domain-failure propagation and validator behaviour using
fakes and in-memory aggregate repositories.

## Next

- Infrastructure: EF Core configurations, tenant query filters, read-repository implementations,
  migrations, outbox.
- Endpoints and UI.
