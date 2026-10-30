# Authorization Revalidation and Access Revocation

This document describes how BSX Platform makes access revocation effective for already
authenticated users, including users with an open Blazor Server circuit. It is the operational and
threat-model companion to [ADR-021](../decisions/ADR-021-Authorization-Revalidation.md).

---

## Threat model

### Assets

- Tenant-scoped CRM, Sales and Inventory data.
- Administration operations over the global identity directory and workspace roles.
- Session and refresh-token material.

### Adversary

An authenticated user whose access has been reduced or removed but who keeps an existing browser
session or an already-open Blazor circuit. The user may:

- keep a tab open and continue invoking component event handlers;
- replay an unexpired access token against the API;
- reuse a cookie after a role, membership, account or session change.

### In-scope threats

| Threat | Example |
| --- | --- |
| Stale circuit principal | A membership is removed while the user has CRM open; the circuit keeps its cached claims. |
| Stale access token | A session is revoked but the JWT remains cryptographically valid until expiry. |
| Privilege retention | A role permission is removed but the cached token still contains it. |
| Account termination bypass | A user is deactivated but a browser tab continues to operate. |
| Session revocation bypass | A session is revoked or all sessions are revoked but an open client keeps working. |

### Out of scope

- An attacker with direct database or process access. Such an attacker can bypass any application
  control; the authorization snapshot still filters permissions, but infrastructure compromise is a
  separate control domain.
- Denial of service against the identity store.
- Token theft by malware on the client device.

---

## Revocation model

Revocation is expressed through authoritative state in the Identity store:

| Event | Authoritative change | Revalidation result |
| --- | --- | --- |
| Membership removed | `TenantMembership.Status = Removed` | Denied |
| Membership role changed | Membership role ids changed | Valid, permissions re-resolved |
| Role permissions changed | Role permission set changed | Valid, permissions re-resolved |
| User deactivated | `User.Status = Disabled`, security stamp rotated | Denied |
| User reactivated | `User.Status = Active`, security stamp rotated | Denied for old sessions; new sign-in required |
| Session revoked | `UserSession.Status = Revoked` | Denied |
| Revoke all sessions | All active sessions revoked | Denied for every session |
| Security stamp changed | Password, email or credential change | Denied |

The token is **not** the authority. The session, account and membership are.

---

## Revalidation strategy

Authorization is revalidated in three complementary layers.

### 1. Per-operation revalidation (authoritative)

Every authenticated CQRS request passes through `AuthorizationBehavior`, which calls
`IAuthorizationRevalidator` **before** the permission gate.

- The web implementation (`WebAuthorizationRevalidator`) reads the current principal and asks
  `IAuthorizationSnapshotReader` for an authoritative snapshot.
- The reader loads the session, user and membership, verifies status, tenant binding and security
  stamp, and re-resolves effective permissions from the current roles.
- The permission gate uses the freshly resolved permission set. A stale claim can never grant a
  permission that the store no longer grants.
- Any failure, timeout or unexpected exception resolves to **denied**.

This covers CRM, Sales and administration operations, whether invoked from an HTTP endpoint or from
a Blazor component.

### 2. Circuit revalidation (framework-aligned)

`BsxRevalidatingAuthenticationStateProvider` derives from the framework's
`RevalidatingServerAuthenticationStateProvider`.

- It revalidates each circuit every `AuthorizationRevalidation:CircuitIntervalSeconds`
  (default **30 seconds**).
- A valid snapshot with changed claims replaces the circuit principal, so the UI, navigation and
  `ICurrentUser` observe the new permissions.
- An invalid snapshot signs the circuit out. `AuthorizeRouteView` renders the not-authorized
  experience and redirects to the sign-in page with `expired=true`.
- An exception during revalidation signs the circuit out (fail closed).

### 3. Cookie revalidation (HTTP requests)

`WebCookieAuthenticationEvents.ValidatePrincipal` revalidates the cookie principal at most every
`AuthorizationRevalidation:CookieIntervalSeconds` (default **30 seconds**, `0` = every request).

- The timestamp is stored in the authentication ticket and renewed with the cookie.
- An invalid session deletes the auth and refresh cookies and redirects to
  `/login?expired=true`.
- The existing near-expiry refresh flow is unchanged and still rotates the access token.

---

## Revocation latency expectations

| Path | Maximum staleness |
| --- | --- |
| Sensitive operation (command or query through the pipeline) | **Immediate** — checked on every request |
| Open Blazor circuit UI | `CircuitIntervalSeconds` (default 30 s) |
| HTTP page request with a cookie | `CookieIntervalSeconds` (default 30 s) |
| Access token used directly against an API operation | **Immediate** for any permission-gated request; the token itself is not revoked early |

The circuit and cookie intervals are configurable. Lower values reduce latency and increase store
reads.

---

## Security guarantees

1. A revoked session, removed membership or deactivated user cannot execute a permission-gated
   operation, even with an unexpired token or a stale circuit principal.
2. A removed permission is not honoured on the next operation, regardless of the token contents.
3. A newly granted permission becomes effective on the next operation.
4. If the authoritative store cannot be read, the operation is denied (fail closed).
5. The circuit principal is replaced when permissions change and signed out when access is removed.
6. Cookie sessions are rejected and cleared when the authoritative session is no longer valid.

---

## Limitations

- **Per-operation store reads.** Every permission-gated request performs a small number of indexed
  reads. This is the cost of immediate revocation. The reader uses `AsNoTracking` and avoids
  loading refresh-token chains. If profiling shows a hot path, a short-lived per-operation cache can
  be added, at the cost of a bounded revocation delay.
- **Circuit timer granularity.** A circuit can continue rendering cached UI for up to the circuit
  interval. Data operations are still blocked immediately by the per-operation check.
- **Multi-instance deployments.** Revalidation reads the shared Identity store, so it works across
  instances. There is no cross-instance push notification; latency is bounded by the configured
  intervals.
- **Platform-only sessions.** A session without a tenant is valid only while the user is active and
  the session is active. Tenant operations require an active membership.
- **Identity store availability.** A store outage denies authenticated operations. This is
  intentional; the alternative is to continue with unverifiable authorization.
- **Legacy/test principals.** Integration tests may build claim-only principals without a session
  id. Production code denies such principals. The test host explicitly opts into claim-based
  authorization for those fixtures only.

---

## Configuration

```json
{
  "AuthorizationRevalidation": {
    "CircuitIntervalSeconds": 30,
    "CookieIntervalSeconds": 30
  }
}
```

- `CircuitIntervalSeconds` is clamped to at least 1 second.
- `CookieIntervalSeconds` of `0` revalidates on every HTTP request.

---

## Verification

Adversarial coverage lives in
`tests/Integration/BSX.Web.IntegrationTests/Security/AuthorizationRevocationTests.cs`:

- membership removal revokes CRM, Sales and administration operations;
- role permission removal applies immediately and retained permissions still work;
- membership role change applies immediately;
- user deactivation revokes access;
- reactivation requires a new sign-in;
- session revocation revokes access;
- revoke-all revokes every open session;
- the circuit provider refreshes permissions and signs out after revocation;
- the cookie path redirects to `/login?expired=true`.
