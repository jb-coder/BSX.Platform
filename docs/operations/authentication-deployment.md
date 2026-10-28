# Authentication Deployment Guidance (D4)

## Scheme

- **JWT bearer**, RS256, validated locally per request (issuer, audience, lifetime, signing key,
  30-second clock skew). No per-request security-stamp lookup (ADR-017).
- Access tokens are short-lived (default 15 minutes); refresh tokens are database-backed and
  rotated on use with a 30-second grace window and reuse detection.

## Deployment checklist

1. Configure `Identity:Jwt:Issuer`, `Audience`, `AccessTokenLifetimeMinutes`, and **shared**
   signing keys from a secret store ([jwt-key-management.md](jwt-key-management.md)).
2. Ensure all instances share the same keys and issuer/audience.
3. Confirm time synchronisation (NTP) across instances and clients — validation allows 30s skew.
4. Terminate TLS in front of the host and keep HSTS enabled (default outside Development).
5. Publish `https://<host>/.well-known/jwks.json` equivalents? Not required — validation keys are
   loaded from configuration. External consumers can obtain validation keys from the same secret
   store distribution mechanism.

## Security-stamp and revocation semantics

- The security stamp is validated **at refresh**, not per request. Password changes, credential
  changes, deactivation and tenant removal rotate the stamp, so existing **refresh** attempts fail
  and the sessions are revoked.
- Because access tokens are short-lived and stateless, a deactivated principal may remain able to
  call APIs for at most one access-token lifetime. Keep the lifetime short.
- Ordinary role/permission changes do not revoke sessions; they take effect on the next token
  issuance (within the access-token TTL) per ADR-018.

## Tenant binding

Access tokens carry the active `tenant_id`. Tenant switching re-issues a token; it is not inferred
from request input. All tenant-scoped operations enforce that the target tenant matches the token's
tenant (`TenantGuard`).

## Client guidance

- Cache the access token until ~30–60s before expiry, then refresh proactively.
- Serialise refreshes per session to avoid consuming the grace window unnecessarily; on `409`
  (concurrent refresh), retry once.
- Do not send refresh tokens over insecure channels; they are bearer credentials.

## Blazor web session

- Browser routes use an HttpOnly cookie session; `/identity`, `/crm`, `/sales` and `/health` keep
  using JWT bearer authentication.
- The web cookie contains identity/permission claims, not raw tokens. The refresh token is held in a
  separate HttpOnly cookie and is rotated by cookie validation.
- **Data Protection keys must be shared** across all web instances (`DataProtection` key ring) so
  authentication cookies remain readable after a rolling deployment or behind a load balancer.
  Persist the key ring in the deployment secret store or a shared protected store.
- Workspace selection is held in an in-process pending store for five minutes. A multi-instance
  deployment must replace it with a distributed store or the selection may fail between nodes.
- Keep TLS enabled; cookies use `Secure` when the request is HTTPS.
