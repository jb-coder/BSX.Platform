# CRM - API Surface

Status: API implemented (v0.3.0 development). Blazor UI, reporting and analytics are out of scope.

## Conventions

- Base path: `/crm`.
- **Authentication is required by default** for every endpoint (JWT bearer, ADR-017).
- **Authorization is permission-based** (`[RequirePermission]` on the Application requests) and
  fail-closed; a missing permission returns `403`.
- **Tenant-aware**: the tenant is resolved from the authenticated token (`ITenantContext`), never from
  client input. Cross-tenant resources are not visible (`404`).
- Result mapping follows ADR-011 (`application/problem+json`).
- Commands persist through the module-aware `CompositeUnitOfWork`; domain events dispatch once.

## Customers

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| GET | `/crm/customers?page=&size=&search=` | `crm.customers.read` | `200` `PagedResult<CustomerSummaryDto>` |
| GET | `/crm/customers/{id}` | `crm.customers.read` | `200` `CustomerDto` |
| POST | `/crm/customers` | `crm.customers.create` | `201` + `Location`, body = id |
| PUT | `/crm/customers/{id}` | `crm.customers.update` | `204` |
| POST | `/crm/customers/{id}/activate` | `crm.customers.update` | `204` |
| POST | `/crm/customers/{id}/deactivate` | `crm.customers.update` | `204` |
| POST | `/crm/customers/{id}/organization` | `crm.customers.update` | `204` |
| DELETE | `/crm/customers/{id}/organization` | `crm.customers.update` | `204` |

- `POST /customers` body: `{ "code", "name", "taxIdentifier" }` (tax identifier optional).
- `PUT /customers/{id}` body: `{ "name", "taxIdentifier" }`.
- `POST /customers/{id}/organization` body: `{ "organizationId" }`.

## Contacts

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| POST | `/crm/customers/{id}/contacts` | `crm.customers.update` | `204` |
| PUT | `/crm/customers/{id}/contacts/{contactId}` | `crm.customers.update` | `204` |
| DELETE | `/crm/customers/{id}/contacts/{contactId}` | `crm.customers.update` | `204` |
| POST | `/crm/customers/{id}/contacts/{contactId}/primary` | `crm.customers.update` | `204` |

- Add body: `{ "name", "email", "phoneNumber", "isPrimary" }` (at least one reachability channel is
  required by the domain; otherwise `400`).
- Update body: `{ "name", "email", "phoneNumber" }`.
- Contacts are owned by the customer aggregate; the API returns `204` and the client re-reads the
  customer to obtain the generated contact id.

## Addresses

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| POST | `/crm/customers/{id}/addresses` | `crm.customers.update` | `204` |
| PUT | `/crm/customers/{id}/addresses/{addressId}` | `crm.customers.update` | `204` |
| DELETE | `/crm/customers/{id}/addresses/{addressId}` | `crm.customers.update` | `204` |
| POST | `/crm/customers/{id}/addresses/{addressId}/default` | `crm.customers.update` | `204` |

- Add/update body: `{ "line1", "line2", "city", "region", "postalCode", "country", "type" }`.
- `type` is `Billing`, `Shipping` or `Other` (case-insensitive); anything else is `400`.
- Uniqueness is `(address, type)`; the first address becomes the default.
- Addresses are owned by the customer aggregate; the API returns `204` and the client re-reads the
  customer to obtain the generated address id.

## Organizations

| Method | Path | Permission | Success |
| --- | --- | --- | --- |
| GET | `/crm/organizations?page=&size=&search=` | `crm.organizations.read` | `200` `PagedResult<OrganizationDto>` |
| GET | `/crm/organizations/{id}` | `crm.organizations.read` | `200` `OrganizationDto` |
| POST | `/crm/organizations` | `crm.organizations.create` | `201` + `Location`, body = id |
| PUT | `/crm/organizations/{id}` | `crm.organizations.update` | `204` |

- Create/update body: `{ "name", "taxIdentifier", "address" }`; `address` is optional and uses
  `{ "line1", "line2", "city", "region", "postalCode", "country" }`. Sending `address: null` on
  update clears it.

## Search semantics

- Customers: case-insensitive partial match on **customer code** or **customer name**.
- Organizations: case-insensitive partial match on **organization name** or **tax identifier**.

## Status codes

| Result | HTTP |
| --- | --- |
| Success (no body) | `204` |
| Success (read) | `200` |
| Created | `201` + `Location` |
| Validation / invalid address type | `400` |
| Unauthenticated | `401` |
| Missing permission / cross-tenant guard | `403` |
| Not found | `404` |
| Conflict (duplicate code, invalid transition, duplicate email/address) | `409` |
| Unavailable | `503` |

Errors are returned as RFC 9457 problem details with the stable `CRM.*` code as the title.

## Health

`AddCrmHealthChecks()` registers `crm-database` with the `ready` tag. `/health/ready` includes the
CRM database alongside the Identity checks; `/health` aggregates all checks.

## Integration notes

- JWT authentication and `ICurrentUser`/`ITenantContext` are provided by the Identity authentication
  pipeline; CRM does not issue tokens.
- Every CRM command resolves through the single module-aware `CompositeUnitOfWork`, which persists
  all module contexts and dispatches each domain event exactly once.
