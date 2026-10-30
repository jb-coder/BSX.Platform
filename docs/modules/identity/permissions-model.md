# Identity - Permissions Model

Permission-based authorization (RBAC) as defined in
[ADR-012](../../decisions/ADR-012-Authorization-Architecture.md). The authorization gate is
always a **permission**; roles are an administration convenience.

---

## Model

```text
User ──(assigned)──► Role ──(grants)──► Permission
 │                                        ▲
 └──────────── effective = union ─────────┘
```

- A **permission** is a stable string `module.resource.action` (for example
  `identity.users.create`).
- A **role** is a named, mutable collection of permissions.
- A **user** is assigned zero or more roles.
- **Effective permissions** of a user are the union of the permissions of all assigned roles.

There are no per-user permission grants. Access is granted through roles only. This keeps the
model auditable and avoids drift.

Every permission also declares a **scope** that states which data boundary it protects and,
therefore, which roles may grant it. Scope is part of the permission contract, not a UI setting
(ADR-020).

---

## Permission Naming

| Rule | Example |
| --- | --- |
| Format `module.resource.action`. | `identity.users.read` |
| Module is the bounded context. | `crm.customers.create` |
| Lowercase, dot-separated. | `identity.roles.set-permissions` |
| Stable and never renamed once published. | Deprecate and add; do not rename. |
| No wildcard permissions. | Never `identity.*` or `*`. |
| Least privilege. | One capability per permission. |

---

## Permission Scope

`PermissionDefinition` carries a `PermissionScope` declared by the owning module
(`BSX.Contracts.Permissions`). The scope is the authorization boundary, not a UI hint.

| Scope | Protects | May be granted by |
| --- | --- | --- |
| `Platform` | Platform-global data (the global user directory and platform role administration). | **Platform roles only** (roles with no tenant, such as the `Administrator` and `Auditor` system roles). |
| `Tenant` | Data confined to the caller's active tenant (CRM, Sales, Inventory, workspace roles and memberships). | Tenant roles **and** platform roles. Tenant data access is still constrained by the tenant guard and tenant query filters. |

Invariant: **a tenant role can never grant a platform-scoped permission.** Platform roles may hold
tenant permissions because the tenant guard and tenant query filters still confine the data they
reach.

Classification follows the code, not intuition:

| Permission family | Scope | Reason |
| --- | --- | --- |
| `identity.users.*` | `Platform` | Handlers read or mutate the global user directory by global user id. |
| `identity.roles.*` | `Tenant` | Role writes are confined to the active tenant by `TenantGuard`; system roles are immutable. Reading a system role exposes only its read-only definition, never tenant data. |
| `identity.permissions.read` | `Tenant` | The catalog is a static, code-declared public contract used to build tenant roles. |
| `identity.memberships.*` | `Tenant` | Membership administration is tenant-confined. |
| `crm.*`, `sales.*`, `inventory.*` | `Tenant` | All module data is tenant-partitioned. |

---

## Permission Catalog

- Permissions are **declared in code** per module and aggregated in `BSX.Contracts.Permissions`.
- Identity reconciles the catalog into the `Administrator` and `Auditor` system roles at startup
  through `SystemRolePermissionSynchronizer` (idempotent).
- Roles may only reference permissions that exist in the catalog; this is enforced on write.
- Adding a permission is additive. Removing a permission requires deprecation and a migration
  that strips it from roles.

### Identity permissions

| Permission | Scope | Guards |
| --- | --- | --- |
| `identity.users.read` | Platform | `GetUserByIdQuery`, `ListUsersQuery` |
| `identity.users.create` | Platform | `RegisterUserCommand` |
| `identity.users.update` | Platform | `UpdateUserCommand` |
| `identity.users.manage-status` | Platform | `ActivateUserCommand`, `DeactivateUserCommand` |
| `identity.users.assign-role` | Platform | Platform role assignment |
| `identity.roles.read` | Tenant | `ListRolesQuery`, `GetRoleByIdQuery` |
| `identity.roles.create` | Tenant | `CreateRoleCommand` |
| `identity.roles.update` | Tenant | `UpdateRoleCommand` |
| `identity.roles.set-permissions` | Tenant | `SetRolePermissionsCommand` |
| `identity.permissions.read` | Tenant | `ListPermissionsQuery` |
| `identity.memberships.read` | Tenant | `ListTenantMembershipsQuery` |
| `identity.memberships.manage` | Tenant | invite, accept, change-role and remove membership |

### CRM permissions

| Permission | Scope | Guards |
| --- | --- | --- |
| `crm.customers.read` | Tenant | Customer queries |
| `crm.customers.create` | Tenant | `CreateCustomerCommand` |
| `crm.customers.update` | Tenant | Customer updates, contacts, addresses and organization assignment |
| `crm.organizations.read` | Tenant | Organization queries |
| `crm.organizations.create` | Tenant | `CreateOrganizationCommand` |
| `crm.organizations.update` | Tenant | `UpdateOrganizationCommand` |

### Sales permissions

| Permission | Scope | Guards |
| --- | --- | --- |
| `sales.leads.read` | Tenant | `GetLeadByIdQuery`, `ListLeadsQuery` |
| `sales.leads.create` | Tenant | `CreateLeadCommand` |
| `sales.leads.update` | Tenant | Lead updates, contacted, qualify and disqualify |
| `sales.opportunities.read` | Tenant | `GetOpportunityByIdQuery`, `ListOpportunitiesQuery` |
| `sales.opportunities.create` | Tenant | `CreateOpportunityFromLeadCommand` |
| `sales.opportunities.update` | Tenant | Opportunity updates, advance, won and lost |

Self-service operations (`me/*`) and authentication operations require authentication but declare
no permission.

### Module catalogs and seeding

Modules publish their codes through `IPermissionCatalogSource` (`BSX.Contracts.Permissions`);
`SalesPermissionCatalog` is the Sales implementation. `AddSalesApplication` registers it.
`SystemRolePermissionSynchronizer` (Identity Infrastructure) reconciles the `Administrator` and
`Auditor` system roles from every registered catalog on startup
(`Identity:Permissions:SynchronizeOnStartup`, default `true`). Synchronization is additive and
idempotent; a missing Identity schema is logged and skipped. Additional modules can publish a
catalog without Identity referencing their implementation.

---

## System Roles

| Role | Permissions | Notes |
| --- | --- | --- |
| `Administrator` | All catalog permissions (platform and tenant). | Cannot be deleted; membership is managed explicitly. |
| `Auditor` | All `*.read` permissions (platform and tenant). | Read-only oversight. |

System roles are platform roles (`TenantId` is null). They may hold tenant permissions because
tenant data access is still confined by the active membership, tenant guard and tenant query
filters. Tenant roles may only hold tenant permissions (ADR-020).

System roles are seeded idempotently. Additional roles are created through
`CreateRoleCommand`, which only creates tenant roles.

---

## Enforcement

- Protected commands and queries declare `[RequirePermission("...")]` (ADR-012).
- `AuthorizationBehavior` evaluates the requirement after validation and before the handler.
- Identity implements `IPermissionChecker` over the user's effective permissions.
- Failures return `Result` with `ErrorKind.Authentication` (401) or `ErrorKind.Permission` (403)
  and are mapped by the adapter (ADR-011).
- Resource-level rules (for example "a user may only edit their own profile") are evaluated in the
  handler using `ICurrentUser`, not by the behavior.

### Permission-scope enforcement (ADR-020)

Scope is enforced in four independent layers. The web UI is a usability aid, never the control.

| Layer | Where | Behaviour |
| --- | --- | --- |
| 1. Catalog membership | `PermissionGrantValidator` on every write path (role create, role permissions, API-key scopes) | An unknown code is rejected with `Identity.Permission.Unknown`; nothing is persisted. |
| 2. Scope validation | `PermissionGrantValidator` + the catalog | A platform-scoped code on a tenant role or tenant-scoped API key is rejected with `Identity.Permission.ScopeForbidden`. The scope is read from the catalog, never from the request. |
| 3. Membership assignment | `AssignRoleToMembershipCommandHandler`, `ChangeMembershipRoleCommandHandler` | A system/platform role cannot be assigned as a workspace membership role; the role must belong to the membership's tenant. |
| 4. Authorization resolution | `PermissionResolver` | Tenant roles contribute only tenant-scoped codes; unknown codes contribute nothing. This is the backstop that keeps authorization safe even if a role row is written outside the application. |

Additional guarantees:

- `CreateRoleCommand` only creates tenant roles; platform roles are owned by
  `SystemRolePermissionSynchronizer`. Platform role creation through the tenant workflow is rejected.
- Tenant role creation and permission edits are tenant-guarded, so a tenant administrator cannot
  create or edit a role in another tenant.
- The administration UI hides platform-scoped permissions from workspace role editors and explains
  why; it also drops legacy platform entries when a tampered role is saved.

### Remediation of existing data

Roles written before ADR-020 may contain platform permissions. They are inert immediately because
`PermissionResolver` filters them, but they should be cleaned up. Audit and remove them with:

```sql
-- Inspect tenant roles that hold platform-scoped permissions.
SELECT r."TenantId", r."Name", p.code
FROM identity.roles AS r
JOIN identity.role_permissions AS p ON p."RoleId" = r."Id"
WHERE r."TenantId" IS NOT NULL
  AND p.code IN (
      'identity.users.read',
      'identity.users.create',
      'identity.users.update',
      'identity.users.manage-status',
      'identity.users.assign-role');
```

Delete the returned `role_permissions` rows, or re-save the role permissions through the
administration UI, which now filters them out.

---

## Claims and Token Staleness

- Effective permissions are carried as `permission` claims in the access token and surfaced by
  `ICurrentUser.Permissions`.
- Because access tokens are stateless and short-lived ([jwt-strategy.md](jwt-strategy.md)),
  permission changes take effect on the next token issuance.
- Security-critical changes (password, credentials, status) rotate the security stamp and revoke
  refresh sessions, forcing re-authentication (ADR-017).
- **Ordinary role/permission changes do not revoke sessions** (ADR-017/ADR-018); they propagate
  within the access-token TTL. `RolePermissionsChanged` and `UserRolesChanged` integration events
  let consumer caches invalidate.
- `GetMyPermissionsQuery` returns the current effective permissions for clients that need to
  render capability-aware UI.

---

## Governance

- Every new protected capability adds a permission to the catalog in the same change.
- Every permission declares a scope; platform-scoped codes must never be grantable by a tenant role.
- Permission codes are treated as public contract; breaking changes require an ADR.
- Permissions are reviewed as part of the feature review checklist
  ([review-agent.md](../../../.opencode/agents/review-agent.md)).
