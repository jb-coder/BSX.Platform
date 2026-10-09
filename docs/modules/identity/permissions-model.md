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

## Permission Catalog

- Permissions are **declared in code** per module and aggregated in `Identity.Contracts`.
- Identity seeds the catalog into `identity.permissions` at startup through
  `SynchronizePermissions` (idempotent).
- Roles may only reference permissions that exist in the catalog; this is enforced on write.
- Adding a permission is additive. Removing a permission requires deprecation and a migration
  that strips it from roles.

### Identity permissions

| Permission | Guards |
| --- | --- |
| `identity.users.read` | `GetUserByIdQuery`, `ListUsersQuery` |
| `identity.users.create` | `RegisterUserCommand` |
| `identity.users.update` | `UpdateUserCommand` |
| `identity.users.manage-status` | `ActivateUserCommand`, `DeactivateUserCommand` |
| `identity.users.lock` | `LockUserCommand`, `UnlockUserCommand` |
| `identity.users.assign-role` | `AssignRoleToUserCommand`, `RemoveRoleFromUserCommand` |
| `identity.users.reset-password` | `ResetUserPasswordCommand` |
| `identity.roles.read` | `ListRolesQuery`, `GetRoleByIdQuery` |
| `identity.roles.create` | `CreateRoleCommand` |
| `identity.roles.update` | `RenameRoleCommand` |
| `identity.roles.set-permissions` | `SetRolePermissionsCommand` |
| `identity.roles.delete` | `DeleteRoleCommand` |
| `identity.permissions.read` | `ListPermissionsQuery` |
| `identity.sessions.read` | `ListUserSessionsQuery` |
| `identity.sessions.revoke` | Admin session revocation |

Self-service operations (`me/*`) and authentication operations require authentication but declare
no permission.

---

## System Roles

| Role | Permissions | Notes |
| --- | --- | --- |
| `Administrator` | All platform permissions. | Cannot be deleted; membership is managed explicitly. |
| `Auditor` | All `*.read` permissions. | Read-only oversight. |

System roles are seeded idempotently. Additional roles are created through
`CreateRoleCommand`.

---

## Enforcement

- Protected commands and queries declare `[RequirePermission("...")]` (ADR-012).
- `AuthorizationBehavior` evaluates the requirement after validation and before the handler.
- Identity implements `IPermissionChecker` over the user's effective permissions.
- Failures return `Result` with `ErrorKind.Authentication` (401) or `ErrorKind.Permission` (403)
  and are mapped by the adapter (ADR-011).
- Resource-level rules (for example "a user may only edit their own profile") are evaluated in the
  handler using `ICurrentUser`, not by the behavior.

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
- Permission codes are treated as public contract; breaking changes require an ADR.
- Permissions are reviewed as part of the feature review checklist
  ([review-agent.md](../../../.opencode/agents/review-agent.md)).
