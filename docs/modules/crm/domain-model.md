# CRM - Domain Model

Status: Domain implemented (v0.3.0 development). Application, Infrastructure, API and UI are out of
scope for this slice.

## Scope

The CRM domain models customers, organizations and contacts. It is persistence-independent: it
references `BSX.SharedKernel` only (no EF Core, BuildingBlocks or other modules).

## Aggregates

### Customer (`AggregateRoot<CustomerId>`)

The consistency boundary for a customer, its addresses and its contact persons.

| Member | Meaning |
| --- | --- |
| `TenantId` | Reserved tenant identifier (multi-tenant ready, ADR-013). |
| `CustomerCode` | Unique business code within a tenant. |
| `CustomerName` | Party name. |
| `TaxIdentifier?` | Optional tax identifier. |
| `OrganizationId?` | Assigned organization, referenced by identifier only. |
| `CustomerStatus` | `Active` / `Deactivated`. |
| `Addresses` | Owned `CustomerAddress` entities. |
| `Contacts` | Owned `ContactPerson` entities. |

Behavior: `Create`, `Activate`, `Deactivate`, `ChangeName`, `ChangeTaxIdentifier`,
`AssignOrganization`, `AddAddress`, `RemoveAddress`, `SetDefaultAddress`, `ChangeAddressType`,
`AddContact`, `RemoveContact`, `SetPrimaryContact`, `ChangeContactDetails`.

Invariants:

- A new customer is `Active`.
- `Activate` is only valid from `Deactivated`; `Deactivate` only from `Active`.
- Organization assignment and adding contacts require an active customer.
- The first address and the first contact person automatically become the default/primary.
- Exactly one default address and one primary contact exist whenever the collection is non-empty.
- Removing the default/primary promotes the first remaining item.
- Contact emails are unique within a customer; addresses are unique by value.
- Limits: 20 addresses and 50 contact persons per customer.

Domain events: `CustomerCreated`, `CustomerActivated`, `CustomerDeactivated`, `OrganizationAssigned`,
`ContactAdded`, `ContactRemoved`.

### Organization (`AggregateRoot<OrganizationId>`)

A grouping entity (for example a holding company). Carries `TenantId`, `CustomerName`,
`TaxIdentifier?` and an optional primary `Address?`. Behavior: `Create`, `Rename`,
`ChangeTaxIdentifier`, `ChangeAddress`. Customers reference organizations by identifier only.

### Contact (`AggregateRoot<ContactId>`)

A standalone contact directory entry. Carries `TenantId`, `CustomerName`, optional `Email`,
`PhoneNumber`, an optional `CustomerId` association, and `ContactStatus` (`Active` / `Inactive`).
Behavior: `Create`, `ChangeName`, `ChangeEmail`, `ChangePhoneNumber`, `AssignToCustomer`,
`RemoveFromCustomer`, `Activate`, `Deactivate`.

## Entities

- **CustomerAddress** (`Entity<CustomerAddressId>`): owned by `Customer`; `Address`, `AddressType`
  (`Billing` / `Shipping` / `Other`) and `IsDefault`.
- **ContactPerson** (`Entity<ContactPersonId>`): owned by `Customer`; `CustomerName`, optional
  `Email` and `PhoneNumber`, and `IsPrimary`.

### Why both `Contact` and `ContactPerson`?

The scope requests a `Contact` aggregate and a `ContactPerson` entity. They model different
concepts:

- `ContactPerson` is a **person embedded in a customer** (an owned child entity of the aggregate).
- `Contact` is a **standalone contact directory aggregate** that may optionally reference a
  customer.

This avoids a navigation graph between aggregates (ADR-003) while satisfying both terms.

## Value Objects

| Value object | Notes |
| --- | --- |
| `CustomerId`, `OrganizationId`, `ContactId` | Strongly typed identifiers (shared `CrmId` base). |
| `ContactPersonId`, `CustomerAddressId` | Identity for owned child entities (supporting types). |
| `TenantId` | Reserved tenant identifier for multi-tenancy (supporting type). |
| `CustomerCode` | Trimmed, upper-case; `[A-Z0-9._-]`, max 32. |
| `CustomerName` | Canonical party name (customer, organization or contact); trimmed, max 200. |
| `TaxIdentifier` | Trimmed, upper-case; `[A-Z0-9./-]`, max 50. |
| `Email` | Trimmed, lower-case; single `@`, max 320. |
| `PhoneNumber` | Trimmed; digits and `+ - ( ) . space`, at least 5 digits, max 32. |
| `Address` | `Line1`, `Line2?`, `City`, `Region?`, `PostalCode?`, `Country`; required line 1, city, country. |

Supporting identifiers (`ContactPersonId`, `CustomerAddressId`) and `TenantId` were added because
child entities require identity and the domain is multi-tenant ready; they introduce no business
capability beyond the requested model.

## Errors

`CrmErrors` is the stable catalog (`CRM.*`). Validation failures use `Error.Validation`; state
conflicts use `Error.Conflict`; missing owned entities use `Error.NotFound`.

## Repositories

Aggregate-specific contracts only: `ICustomerRepository`, `IOrganizationRepository`,
`IContactRepository`. Each exposes `GetByIdAsync` and `AddAsync`; `ICustomerRepository` also exposes
`ExistsByCodeAsync(TenantId, CustomerCode)` for tenant-scoped code uniqueness.

## Out of scope / next

- Application commands/queries and FluentValidation.
- Persistence (EF Core configuration, migrations, soft delete, auditing shadow properties).
- Endpoints and UI.
- Integration events (for example `CustomerCreatedIntegrationEvent`) through the Outbox.
