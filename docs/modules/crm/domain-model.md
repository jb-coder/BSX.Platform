# CRM - Domain Model

Status: Domain implemented and hardened (v0.3.0 development). Application, Infrastructure, API and
UI are out of scope for this slice.

## Scope

The CRM domain models customers, organizations and customer contact persons. It is
persistence-independent: it references `BSX.SharedKernel` only (no EF Core, BuildingBlocks or other
modules).

---

## Resolved domain review decisions

### 1. Tenant read isolation - **Option A** (tenant in read contracts)

Aggregate read contracts take a `TenantId`:

```csharp
Task<Customer?> GetByIdAsync(TenantId tenantId, CustomerId id, CancellationToken ct = default);
Task<Organization?> GetByIdAsync(TenantId tenantId, OrganizationId id, CancellationToken ct = default);
Task<bool> ExistsByCodeAsync(TenantId tenantId, CustomerCode code, CancellationToken ct = default);
```

**Rationale**

- CRM aggregates are **tenant-owned** (unlike global Identity users). A read without a tenant is a
  cross-tenant leak waiting to happen.
- The contract makes cross-tenant access impossible to express accidentally; the compiler enforces
  the tenant argument.
- It keeps the domain explicit and transport-agnostic while allowing a per-tenant database model
  later without changing the contract shape.
- Infrastructure **must still** apply a tenant query filter as defence in depth (ADR-013), but that
  is now a secondary guarantee, not the only one.

A domain test (`RepositoryContractTests`) locks this shape.

### 2. Contact boundary - `ContactPerson` is the only contact model

The standalone `Contact` aggregate has been **removed**. `ContactPerson` is the canonical
representation of a customer's contact.

**Rationale**

- The glossary defines a Contact as "a person associated with a customer" - exactly `ContactPerson`.
- Keeping both `Customer.Contacts` (owned `ContactPerson`) and a separate `Contact` aggregate with
  `CustomerId` created two sources of truth for the same concept and forced queries to union two
  models.
- `ContactPerson` as an owned entity lets the `Customer` aggregate enforce the important invariants
  atomically: a single primary contact, per-customer email uniqueness, and the contact limit.
- A future **global contact directory** (people shared across customers/tenants) is a different
  concept. If it is ever needed it should be introduced as a new aggregate with its own ADR, not
  conflated with customer contacts.

### 3. Address uniqueness - **`(Address, Type)`**

A customer may hold the same physical address for different purposes (e.g. Billing and Shipping),
but may not hold the same `(address, type)` pair twice.

**Rationale**

- Uniqueness on `Address` alone wrongly prevented a legitimate Billing + Shipping address.
- No uniqueness would allow accidental duplicate rows.
- `(Address, Type)` mirrors how addresses are actually used and keeps the collection clean.
- `AddAddress` and `ChangeAddressType` both enforce the rule; the existing single-default and
  removal-promotion invariants are unchanged.

---

## Additional decisions

### Active-state mutation policy

| Operation | Requires `Active`? |
| --- | --- |
| `AssignOrganization` | Yes - operational relationship change |
| `AddContact` | Yes - operational relationship change |
| `ChangeName`, `ChangeTaxIdentifier` | No - administrative correction |
| `AddAddress`, `RemoveAddress`, `SetDefaultAddress`, `ChangeAddressType` | No - administrative data |
| `ChangeContactDetails`, `SetPrimaryContact`, `RemoveContact` | No - administrative data |
| `ClearOrganization` | No - administrative correction |
| `Activate`, `Deactivate` | n/a - lifecycle operations |

Deactivated customers remain correctable/archivable; operational changes that extend the customer
relationship are blocked. This policy is covered by tests.

### Contact reachability

A contact person must have **at least one** reachability channel: an `Email` or a `PhoneNumber`.
`AddContact` and `ChangeContactDetails` reject a contact with neither (`CRM.Customer.ContactUnreachable`).
This prevents unusable contact records.

### Contact email uniqueness

Contact emails are unique **within a customer** (case-insensitive via the normalized `Email` value
object). The same email may be used for contacts of different customers or in different tenants.
There is no global contact directory, so no global uniqueness rule exists.

### Organization detachment

`Customer.ClearOrganization()` detaches an assigned organization and raises
`OrganizationUnassigned`. It fails with `CRM.Customer.OrganizationNotAssigned` when no organization
is assigned, and is allowed in any customer status (administrative correction). Reassignment to a
different organization remains `AssignOrganization` (raises `OrganizationAssigned`).

---

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
`AssignOrganization`, `ClearOrganization`, `AddAddress`, `RemoveAddress`, `SetDefaultAddress`,
`ChangeAddressType`, `AddContact`, `RemoveContact`, `SetPrimaryContact`, `ChangeContactDetails`.

Invariants:

- A new customer is `Active`.
- `Activate` is only valid from `Deactivated`; `Deactivate` only from `Active`.
- Organization assignment and adding contacts require an active customer.
- The first address and the first contact person automatically become the default/primary.
- Exactly one default address and one primary contact exist whenever the collection is non-empty;
  removing the default/primary promotes the first remaining item.
- Address uniqueness is `(Address, Type)`.
- Contact emails are unique per customer; contacts must have an email or phone.
- Limits: 20 addresses and 50 contact persons per customer.

Domain events: `CustomerCreated`, `CustomerActivated`, `CustomerDeactivated`, `OrganizationAssigned`,
`OrganizationUnassigned`, `ContactAdded`, `ContactRemoved`.

### Organization (`AggregateRoot<OrganizationId>`)

A grouping entity (for example a holding company). Carries `TenantId`, `CustomerName`,
`TaxIdentifier?` and an optional primary `Address?`. Behavior: `Create`, `Rename`,
`ChangeTaxIdentifier`, `ChangeAddress`. Customers reference organizations by identifier only.

---

## Entities

- **CustomerAddress** (`Entity<CustomerAddressId>`): owned by `Customer`; `Address`, `AddressType`
  (`Billing` / `Shipping` / `Other`) and `IsDefault`.
- **ContactPerson** (`Entity<ContactPersonId>`): owned by `Customer`; `CustomerName`, optional
  `Email` and `PhoneNumber`, and `IsPrimary`.

## Value Objects

| Value object | Notes |
| --- | --- |
| `CustomerId`, `OrganizationId` | Strongly typed identifiers (shared `CrmId` base). |
| `ContactPersonId`, `CustomerAddressId` | Identity for owned child entities (supporting types). |
| `TenantId` | Reserved tenant identifier for multi-tenancy (supporting type). |
| `CustomerCode` | Trimmed, upper-case; `[A-Z0-9._-]`, max 32. |
| `CustomerName` | Canonical party name (customer, organization or contact); trimmed, max 200. |
| `TaxIdentifier` | Trimmed, upper-case; `[A-Z0-9./-]`, max 50. |
| `Email` | Trimmed, lower-case; single `@`, max 320. |
| `PhoneNumber` | Trimmed; digits and `+ - ( ) . space`, at least 5 digits, max 32. |
| `Address` | `Line1`, `Line2?`, `City`, `Region?`, `PostalCode?`, `Country`; required line 1, city, country. |

`CustomerName` is used for customers, organizations and contact persons as the shared party-name
value object. It is a candidate rename (`PartyName`) before the Application/API layers expose it.

## Errors

`CrmErrors` is the stable catalog (`CRM.*`). Validation failures use `Error.Validation`; state
conflicts use `Error.Conflict`; missing owned entities use `Error.NotFound`.

## Repositories

Aggregate-specific, tenant-scoped reads:

- `ICustomerRepository`: `GetByIdAsync(TenantId, CustomerId)`, `ExistsByCodeAsync(TenantId, CustomerCode)`, `AddAsync`.
- `IOrganizationRepository`: `GetByIdAsync(TenantId, OrganizationId)`, `AddAsync`.

There is no `IContactRepository`: contact persons are owned by `Customer`.

## Out of scope / next

- Application commands/queries and FluentValidation.
- Persistence (EF Core configuration, migrations, soft delete, auditing shadow properties, tenant
  query filters).
- Endpoints and UI.
- Integration events through the Outbox (including a future global contact directory if required).
