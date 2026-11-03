# Authentication Deployment Guidance (D4)

## Scheme

- **JWT bearer**, RS256, validated locally per request (issuer, audience, lifetime, signing key,
  30-second clock skew).
- Access tokens are short-lived (default 15 minutes); refresh tokens are database-backed and
  rotated on use with a 30-second grace window and reuse detection.
- Permission-gated operations additionally revalidate the session, account, membership and security
  stamp against the Identity store before executing (ADR-021). The token is never the authority.

## Deployment checklist

1. Configure `Identity:Jwt:Issuer`, `Audience`, `AccessTokenLifetimeMinutes`, and **shared**
   signing keys from a secret store ([jwt-key-management.md](jwt-key-management.md)).
2. Ensure all instances share the same keys and issuer/audience.
3. Confirm time synchronisation (NTP) across instances and clients - validation allows 30s skew.
4. Terminate TLS in front of the host and keep HSTS enabled (default outside Development).
5. Configure forwarded headers: the proxy must send `X-Forwarded-Proto`, `X-Forwarded-Host` and
   `X-Forwarded-For`, and `ReverseProxy:KnownProxies`/`KnownNetworks` must trust the proxy hop.
6. Configure `DataProtection:ApplicationName` and a persistent, shared `DataProtection:KeysPath`.
   Outside Development the host fails fast if the path is missing or not writable.
7. Set `AllowedHosts` to the public host name(s). Outside Development a wildcard entry anywhere in
   the semicolon-separated list, and a loopback-only development list, are rejected.
8. Publish `https://<host>/.well-known/jwks.json` equivalents? Not required - validation keys are
   loaded from configuration. External consumers can obtain validation keys from the same secret
   store distribution mechanism.

## Security-stamp and revocation semantics

- The security stamp is validated **on every permission-gated operation** by the authorization
  revalidator, and again at refresh. Password changes, credential changes, deactivation, lockout
  and tenant removal rotate the stamp, so existing sessions and refresh tokens are rejected.
- A deactivated principal cannot execute a permission-gated operation, even with an unexpired
  access token or an open Blazor circuit. Revocation is immediate for operations; stale UI may
  remain visible for at most the circuit interval.
- Role and permission changes do not necessarily revoke the session. They are observed on the next
  revalidated operation because permissions are re-resolved from the store, not read from the token
  (ADR-021).

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
- **Data Protection keys are persistent and shared** across all web instances. Set
  `DataProtection:ApplicationName` to the same value on every instance and `DataProtection:KeysPath`
  to a persistent, writable, shared directory. Outside Development the host fails fast when the path
  is missing or unwritable. The `data-protection` readiness check protects and unprotects a payload,
  so a broken key ring fails `/health/ready`. See
  [deployment-hardening.md](../deployment/deployment-hardening.md).
- **Cookies.** The authentication, refresh, antiforgery and pending-selection cookies are
  `HttpOnly`. Outside Development `Web:Cookies:SecurePolicy` defaults to `Always`, so a proxy that
  forgets `X-Forwarded-Proto` cannot downgrade the cookies. `SameSite` defaults are `Lax` for the
  authentication cookie and `Strict` for the refresh, antiforgery and pending-selection cookies.
- Workspace selection is held in an in-process pending store for five minutes. The store contains
  no password; the URL carries only a non-secret selection handle and the secret is held in the
  `BSX.Web.PendingLogin` HttpOnly cookie. A multi-instance deployment still needs a distributed
  store or session affinity for the selection window.
- Keep TLS enabled and make sure the proxy forwards the original scheme; the application redirects
  HTTP to HTTPS by default and must not create a redirect loop.
