# ADR-012 - Authorization Architecture

## Status

Accepted

---

## Context

Identity is the next module and will provide authentication, authorization, RBAC and permissions
(module-catalog). Before Identity starts, the platform must define how **authorization is
consumed** so modules can be written against a stable seam without waiting for Identity.

The foundation review identified:

- `ICurrentUser` had no implementation and was not registered — a DI trap.
- There was no authorization behavior in the CQRS pipeline.
- No decision existed on roles vs permissions, on where checks run, or on default behavior before
  Identity is installed.

Constraints:

- Authentication (JWT, refresh tokens) is out of scope; this ADR defines the consumer side only.
- Failures must be returned as values, not exceptions (Rule 5, ADR-011).
- Modules must not depend on Identity's implementation; they depend on abstractions only.

---

## Decision

Authorization is **permission-based**. Roles are, from the consumer's point of view, named
collections of permissions resolved by Identity. The CQRS pipeline enforces authorization
declaratively using requirements colocated with each vertical slice.

### 1. Contracts (application-facing, in `BSX.BuildingBlocks.Authorization`)

| Contract | Responsibility |
| --- | --- |
| `ICurrentUser` | Identity of the caller: `UserId`, `Email`, `IsAuthenticated`, `Permissions`, reserved `TenantId`. |
| `IPermissionChecker` | Evaluates whether the current user holds a required permission; returns a `Result`, not a boolean, so failures map cleanly (ADR-011). |
| `RequirePermissionAttribute` | Declares the permission a command/query requires. Applied to the request type. |

- `ICurrentUser` is an abstraction; the host implements it from the authenticated principal.
- Modules never reference Identity; they reference these abstractions.

### 2. Permission model

- A permission is a granular string in the form `resource.action` (for example,
  `crm.customers.create`).
- Permissions are granted to roles; roles are assigned to users (owned by Identity).
- The gate is the permission, not the role. Roles exist for administration.
- Permission names are stable and namespaced; they are published contracts.

### 3. Pipeline position (canonical order)

```text
Logging → Validation → Authorization → Transaction (commands) → Handler
```

- Authorization runs **after** validation (invalid input is rejected before permissions are
  evaluated) and **before** the handler and any transaction.
- Queries are authorized the same way as commands; authorization is orthogonal to state changes.
- Requests with no `RequirePermission` are allowed through by the behavior.

### 4. Defaults (no DI trap, no insecure default)

- `ICurrentUser` is always registered. The default is an **anonymous** implementation; the host
  replaces it with a `ClaimsPrincipal`-backed implementation.
- `IPermissionChecker` defaults to **fail-closed**: if a request declares a permission and no
  Identity-backed checker is registered, the request is denied with `ErrorKind.Permission`.
- The `AuthorizationBehavior` is a no-op when the request declares no requirement, so modules can
  be developed and tested before Identity exists.
- Allow-all is explicitly rejected as a default.

### 5. Failure semantics

- Authentication failure → `ErrorKind.Authentication` (401).
- Authorization failure → `ErrorKind.Permission` (403).
- Failures are returned as `Result`; the behavior never throws.

### 6. Authentication vs authorization

- This ADR defines consumption. Identity defines issuance and validation:
  token format, claims, refresh, revocation.
- The host maps the authenticated principal's claims to `ICurrentUser.Permissions`. The reserved
  claim type for permissions is `permission`.

### 7. Complex rules

- The behavior handles coarse permission gates only.
- Resource-level rules (ownership, tenant isolation, entity state) are evaluated by the domain or
  application handler using `ICurrentUser`, because they require domain knowledge.
- Multi-tenancy is supported by the reserved `TenantId` and global query filters (ADR-010), not
  by the authorization behavior.

### 8. Auditing integration

- The auditing actor (ADR-010) is `ICurrentUser.UserId`, falling back to `system`.

---

## Alternatives Considered

### Role-based checks at the gate

Pros

- Simple.

Cons

- Not granular; role changes ripple through code.
- Violates the permission model defined for Identity.

### Allow-all default until Identity is installed

Pros

- Nothing breaks during development.

Cons

- Insecure by default; a forgotten registration ships an open system.
- Hides missing registration until production.

### Throw exceptions on authorization failure

Pros

- Familiar.

Cons

- Violates Rule 5; complicates the pipeline and transport mapping.

### Centralized external policy service

Pros

- Enterprise-grade.

Cons

- Premature for a modular monolith; operational overhead (Rule 8, Rule 10).

---

## Consequences

### Positive

- A stable authorization seam exists before Identity.
- Modules are decoupled from Identity's implementation.
- Fail-closed default is secure even before Identity is installed.
- Authorization is declarative, visible next to the operation it protects, and testable.
- Failures map cleanly to 401/403 via ADR-011.

### Negative

- Every protected operation must declare a permission (a disciplined convention).
- Identity must implement `IPermissionChecker` and remove the anonymous/fail-closed defaults at
  composition time.

---

## Notes

- Referenced by: [ADR-010](ADR-010-Auditing-Strategy.md),
  [ADR-011](ADR-011-Result-And-Error-Mapping-Strategy.md),
  [authorization.md](../diagrams/authorization.md),
  [implementation-guidelines.md](../architecture/implementation-guidelines.md).
