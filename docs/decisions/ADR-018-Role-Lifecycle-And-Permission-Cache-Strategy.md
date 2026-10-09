# ADR-018 - Role Lifecycle and Permission Cache Strategy

## Status

Accepted

**Amends:** `docs/modules/identity/permissions-model.md`.

---

## Context

The Principal Architect Review identified three lifecycle gaps:

1. `RoleDeleted` leaves dangling `RoleId` references.
2. System roles (`Administrator` = all permissions, `Auditor` = all reads) are inconsistent with
   the "no wildcard" rule; they must be re-synchronized whenever permissions are added.
3. There is no defined invalidation strategy when roles or permissions change.

---

## Decision

### 1. Role deletion strategy

- Roles are **tenant-scoped** (ADR-013) and are **soft-deleted** (ADR-010) to preserve history.
- Deletion is **blocked** (returns `Conflict`) while the role is assigned to any active
  membership, unless the command supplies a **reassignment target** role.
- **System roles cannot be deleted or renamed.**
- Deleting a role never physically removes it; it is marked deleted and excluded from assignment.

### 2. System role synchronization

- System roles are **declarative**, not user-edited.
- Their permission sets are defined by **selectors** evaluated at synchronization time:
  - `Administrator` → all permissions in the catalog.
  - `Auditor` → all permissions whose action is a read.
- `SynchronizePermissions` (idempotent, at startup) reconciles the catalog and every system role:
  adds newly declared permissions, removes deprecated ones.
- System roles therefore stay correct as modules add permissions, without manual edits.

### 3. Permission cache invalidation

- Effective permissions are embedded in short-lived access tokens; the natural propagation window
  is the access-token TTL (≤15 minutes, ADR-017).
- Identity maintains a **permission catalog cache**; it is invalidated when
  `SynchronizePermissions` runs.
- Role/permission changes publish integration events through the Outbox so consumers that cache
  authorization data can invalidate:
  - `RolePermissionsChangedIntegrationEvent` (role-level change).
  - `UserRolesChangedIntegrationEvent` (membership-level change).
- **Ordinary** role/permission changes do **not** revoke sessions (ADR-017). Only password and
  status changes revoke.

### 4. Assignment consistency

- Assigning/removing a role is a command on the owning aggregate (`TenantMembership` for user
  roles; `ServiceAccount` for machine roles).
- Concurrent membership updates use optimistic concurrency.
- A role's tenant must match the membership's tenant, or the role must be a system role.

---

## Alternatives Considered

- **Hard-delete roles with cascade removal from memberships** — rejected: destroys history and is
  error-prone.
- **Block deletion even with reassignment** — rejected: too rigid for operations.
- **Wildcard permissions for system roles** — rejected in ADR-012 (no wildcards); selectors give
  the same outcome without special-case authorization logic.
- **Revoke sessions on permission change** — rejected (ADR-017): disruptive.

---

## Consequences

### Positive

- No dangling role references; role history is preserved.
- System roles remain correct as the permission catalog grows.
- Cache invalidation is defined for both token-embedded and external caches.
- Routine permission changes do not log users out.

### Negative

- Permission propagation is eventually consistent (bounded by token TTL).
- Role deletion may require an explicit reassignment step.

---

## Future Considerations

- Permission hierarchy or grouping to simplify large role definitions (without wildcards).
- Policy-based conditions (resource scoping) for SaaS, on top of permissions.
- A permission-change audit trail feeding the security audit log.
