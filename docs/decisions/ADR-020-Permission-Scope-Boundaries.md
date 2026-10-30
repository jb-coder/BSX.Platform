# ADR-020 - Permission Scope Boundaries

## Status

Accepted

---

## Context

The platform is a multi-tenant modular monolith with permission-based authorization
([ADR-012](ADR-012-Authorization-Architecture.md)) and tenant memberships
([ADR-013](ADR-013-Multi-Tenant-Identity-Strategy.md)). Permissions are declared per module and
aggregated into a single platform catalog ([ADR-018](ADR-018-Role-Lifecycle-And-Permission-Cache-Strategy.md)).

A security review found that the catalog did not distinguish between capabilities over
**platform-global data** and capabilities over **tenant-confined data**. Role write paths validated
only the permission code syntax, and `PermissionResolver` unioned the permissions of platform roles
and tenant roles into one flat set.

That allowed a tenant administrator to:

1. create or edit a tenant role and grant a global Identity permission such as
   `identity.users.read`;
2. assign that role to themselves or another member;
3. receive the global permission in the next access token and operate on the global user directory
   across tenants.

The membership assignment path also allowed a platform system role to be attached to a tenant
membership. The web UI offered the full catalog and the platform role list, but UI filtering alone
is not a security boundary.

The platform therefore needs a formal, catalog-driven scope model that is enforced by the backend
and remains safe even if role data is written outside the application.

---

## Decision

Every permission declares a `PermissionScope` in its module catalog
(`BSX.Contracts.Permissions.PermissionDefinition`).

| Scope | Protects | Grantable by |
| --- | --- | --- |
| `Platform` | Platform-global data, such as the global user directory and platform role administration. | Platform roles only (roles with `TenantId == null`, such as the `Administrator` and `Auditor` system roles). |
| `Tenant` | Data confined to the active tenant: CRM, Sales, Inventory, workspace roles and memberships. | Tenant roles and platform roles. Tenant data access remains confined by `TenantGuard` and tenant query filters. |

The invariant is: **a tenant role can never grant a platform-scoped permission.**

### Classification

Scope is derived from what the guarded code operates on, not from module ownership:

- `identity.users.*` are platform-scoped because their handlers read or mutate the global user
  directory by global user id.
- `identity.roles.*`, `identity.permissions.read` and `identity.memberships.*` are tenant-scoped
  because role writes are confined to the active tenant by `TenantGuard`, system roles are
  immutable, and reading a system role exposes only its read-only definition, never tenant data.
- All `crm.*`, `sales.*` and `inventory.*` permissions are tenant-scoped because their data is
  tenant-partitioned.

Platform roles may hold tenant permissions. This is intentional: platform administrators need
module access, and tenant guards and query filters still restrict them to the tenant they selected.

### Enforcement layers

1. **Catalog membership** - `PermissionGrantValidator` rejects any code not declared by the
   catalog (`Identity.Permission.Unknown`). This applies to role creation, role permission edits and
   tenant-scoped API-key scopes.
2. **Scope validation** - the same validator rejects platform-scoped codes on tenant roles and
   tenant-scoped API keys (`Identity.Permission.ScopeForbidden`). The scope is read from the
   catalog, never from the request.
3. **Membership validation** - `AssignRoleToMembershipCommandHandler` and
   `ChangeMembershipRoleCommandHandler` reject system/platform roles and roles from another tenant.
   `CreateRoleCommand` only creates tenant roles and is tenant-guarded.
4. **Authorization resolution** - `PermissionResolver` is the final backstop. A tenant role
   contributes only tenant-scoped codes, and unknown codes contribute nothing. A role row written
   through direct repository or database manipulation therefore cannot escalate a principal.

### Client behaviour

`PermissionDto` exposes the scope. The administration role editor shows only the scope it may
grant, hides platform permissions from workspace role editors and explains why. The UI is a
usability aid; the backend remains authoritative.

---

## Alternatives Considered

### Infer scope from the permission code prefix

Pros

- No catalog change; `identity.*` is treated as platform, everything else as tenant.

Cons

- Wrong for tenant-confined Identity capabilities (`identity.roles.*`, `identity.memberships.*`)
  and would break workspace role administration.
- Couples security policy to string parsing and silently changes meaning when a module adds a code.

### Filter the catalog in the UI only

Pros

- Smallest change.

Cons

- Not a security boundary. Any direct API or application-layer call bypasses it.
- Provides no protection against existing or tampered role rows.

### Separate permission namespaces per tenant

Pros

- Strong isolation at the code level.

Cons

- Duplicates the catalog per tenant and makes platform-wide system roles unmanageable.
- Does not solve the global-user permission problem by itself.

### Database-per-tenant isolation

Pros

- Strong physical isolation.

Cons

- Premature for the current modular monolith, operationally expensive, and does not prevent a
  tenant principal from receiving a global capability in a shared Identity database.

---

## Consequences

### Positive

- Tenant administrators cannot obtain platform capabilities through role creation, editing,
  membership assignment or direct persistence manipulation.
- Unknown permissions can no longer be persisted on roles or tenant-scoped API keys.
- The rule is declared once per permission and enforced in four layers, including a resolver
  backstop.
- Existing CRM and Sales authorization continues to work unchanged.
- Platform administrators keep global access through platform roles.

### Negative

- Every permission definition must declare a scope; adding a permission is no longer just a code.
- Platform roles intentionally hold tenant permissions, so the model relies on `TenantGuard` and
  tenant query filters for tenant data confinement. This dependency is documented and tested.
- Roles written before this decision may still contain platform permissions. They are inert at
  authorization time and are cleaned up through the documented remediation query or a role save.

---

## Notes

- Referenced by: [permissions-model.md](../modules/identity/permissions-model.md),
  [ADR-012](ADR-012-Authorization-Architecture.md),
  [ADR-018](ADR-018-Role-Lifecycle-And-Permission-Cache-Strategy.md).
- Adversarial coverage lives in
  `tests/Integration/BSX.Web.IntegrationTests/Security/PermissionScopeBoundaryTests.cs` and
  `tests/Modules/Identity/BSX.Modules.Identity.Application.UnitTests/Authentication/PermissionResolverTests.cs`.
