# ADR-017 - Token Invalidation, Security Stamp and Session Strategy

## Status

Accepted

**Amends:** `docs/modules/identity/jwt-strategy.md` and
`docs/modules/identity/refresh-token-strategy.md`.

---

## Context

The Principal Architect Review identified three related issues:

1. `SecurityStamp` was described as checked per request against current state, which contradicts
   the stateless-JWT model and implies a database/cache lookup on every request.
2. Refresh-token reuse detection with no grace window causes avoidable session revocation on
   legitimate concurrency.
3. Role/permission changes revoking all sessions is disruptive (thundering-herd re-login) and is
   heavier than necessary given short access-token lifetimes.

This ADR defines the token invalidation model, the security-stamp strategy and the session
management strategy.

---

## Decision

### 1. Token invalidation model

- **Access tokens are stateless and short-lived** (default 15 minutes). They are **not** validated
  against the database or a cache per request.
- The **revocation unit is the session** (the refresh chain), not the access token.
- The security stamp is validated **at refresh time**, not per request.
- An emergency **`jti` deny-list** exists for incidents only; it is not part of the normal path.

### 2. Security stamp strategy

- The security stamp (token version) is an opaque value stored on the principal.
- It rotates on **security-relevant changes**: password change/reset, credential add/remove,
  status change (deactivate/lock), and removal from a tenant.
- A rotated stamp causes **refresh to fail**, forcing re-authentication once the short access token
  expires.
- It is embedded in the access token as `sstamp` for **diagnostics and refresh validation**, not
  per-request enforcement.

### 3. Invalidation matrix

| Event | Access token | Refresh chain | Notes |
| --- | --- | --- | --- |
| Access token expiry | n/a | unaffected | Default ≤15 min. |
| Logout | expires (short) | current session revoked | Immediate for refresh. |
| Logout all | expires (short) | all sessions revoked | |
| Password change/reset | expires (short) | all sessions revoked + stamp rotated | Forces re-auth. |
| Deactivate/lock user | expires (short) | all sessions revoked + stamp rotated | |
| Removed from tenant | tenant tokens invalid at refresh | tenant sessions revoked | Tenant-scoped. |
| Role/permission change | expires (short) | **unchanged** | Propagates within token TTL; see ADR-018. |
| Refresh token reuse | n/a | affected chain revoked | Security event. |
| Incident (emergency) | `jti` deny-list | optional | Operational only. |

### 4. Session management strategy

- **One session per (principal, device/context)**; a session owns a refresh chain.
- **Absolute maximum:** 30 days. **Idle timeout:** 7 days since last refresh.
- **Session cap:** default 10 active sessions per principal; exceeding it evicts the oldest.
- **Tenant binding:** sessions are principal-scoped; the active tenant is a token property
  (ADR-013), so switching tenant does not create a session.
- **Rotation grace:** the immediately previous refresh token is accepted for a short grace window
  (default 30 seconds) to absorb legitimate races; presenting it after the grace triggers reuse
  detection. Clients should still serialize refresh (single-flight).

---

## Alternatives Considered

- **Per-request security-stamp lookup** — rejected: reintroduces state, adds DB/cache load, and
  contradicts the stateless model.
- **Long-lived access tokens with instant revocation** — rejected: larger exposure window and
  stateful validation.
- **Revoke all sessions on any permission change** — rejected for ordinary changes: disruptive at
  scale. Reserved for security-critical changes.
- **No rotation grace** — rejected: false-positive reuse detection and forced re-login.

---

## Consequences

### Positive

- The stateless model is honest: no hidden per-request state.
- Revocation is predictable and bounded by the short access-token lifetime.
- Security-critical changes force re-authentication; routine permission changes do not log users
  out.
- Concurrent refresh no longer spuriously revokes sessions in normal operation.

### Negative

- Permission/role changes take effect within the access-token TTL, not instantly.
- Immediate access-token revocation requires the emergency deny-list.

---

## Future Considerations

- Distributed **token-version cache** (for example Redis) if near-instant revocation is required at
  scale, without changing the token contract.
- Configurable idle/absolute/cap per tenant (SaaS policy).
- Device binding and risk-based step-up in the MFA phase (ADR-014).
