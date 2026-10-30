# ADR-021 - Authorization Revalidation and Access Revocation

## Status

Accepted

---

## Context

The web application uses cookie authentication, short-lived stateless JWT access tokens and
Blazor Server circuits ([ADR-012](ADR-012-Authorization-Architecture.md),
[ADR-017](ADR-017-Token-Invalidation-And-Session-Strategy.md)). Blazor's default
`ServerAuthenticationStateProvider` caches the principal for the lifetime of the circuit, and the
CQRS authorization behavior evaluated only the cached permission claims.

A security review found that an already-open circuit could continue operating after:

- a membership was removed or its role changed;
- a role's permissions changed;
- a user was deactivated;
- a session was revoked, including "revoke all sessions";
- the security stamp changed.

Access revocation must actually revoke access. The system must fail closed when authorization
freshness cannot be verified.

---

## Decision

Authorization is revalidated from the authoritative Identity store, not from cached claims, in three
layers.

### 1. Per-operation revalidation

`AuthorizationBehavior` calls `IAuthorizationRevalidator` before the permission gate for every
authenticated request. The web implementation reads an `AuthorizationSnapshot` from
`IAuthorizationSnapshotReader`, which verifies:

- the session exists, belongs to the user, is active and not expired;
- the session tenant matches the principal;
- the user is active;
- the session security stamp matches the user's current security stamp;
- the membership exists and is active when the session is tenant-bound;
- the effective permissions are re-resolved from the current roles.

The freshly resolved permission set is authoritative. Any error, timeout or missing state resolves
to denied.

### 2. Circuit revalidation

`BsxRevalidatingAuthenticationStateProvider` derives from the framework's
`RevalidatingServerAuthenticationStateProvider` and revalidates every circuit at a configurable
interval. Valid snapshots with changed claims replace the circuit principal; invalid snapshots sign
the circuit out.

### 3. Cookie revalidation

`WebCookieAuthenticationEvents` revalidates the cookie principal at a configurable interval and
replaces the principal with a fresh snapshot. Invalid sessions delete the auth and refresh cookies
and redirect to the sign-in page with `expired=true`.

### Configuration

`AuthorizationRevalidation:CircuitIntervalSeconds` and
`AuthorizationRevalidation:CookieIntervalSeconds` default to 30 seconds. `0` revalidates on every
HTTP request.

---

## Alternatives Considered

### Rely on access-token expiry

Pros

- No additional store reads.

Cons

- Leaves a revocation window of the full token lifetime.
- Does not address an open circuit whose principal never expires.

### Rely on the circuit timer only

Pros

- Minimal code.

Cons

- A circuit can invoke event handlers between ticks.
- Does not protect API operations.

### Short-lived tokens only

Pros

- Simple mental model.

Cons

- Still leaves a window and does not solve the cached circuit principal.

### Push revocation notifications to circuits

Pros

- Lowest latency.

Cons

- Requires distributed infrastructure and connection tracking; the modular monolith does not need
  it yet.

---

## Consequences

### Positive

- Revocation is effective for open circuits, cookies and API operations.
- Removed permissions disappear and new permissions take effect on the next operation.
- The model fails closed when the store cannot be read.
- The circuit revalidation uses the framework's revalidation provider instead of custom timers.

### Negative

- Permission-gated requests perform a small number of indexed identity reads.
- A circuit can render stale UI for up to the circuit interval; data operations remain blocked.
- A store outage denies authenticated operations (intentional fail-closed behaviour).

---

## Notes

- Referenced by: [authorization-revalidation.md](../security/authorization-revalidation.md),
  [permissions-model.md](../modules/identity/permissions-model.md),
  [ADR-017](ADR-017-Token-Invalidation-And-Session-Strategy.md).
- Adversarial tests: `tests/Integration/BSX.Web.IntegrationTests/Security/AuthorizationRevocationTests.cs`.
