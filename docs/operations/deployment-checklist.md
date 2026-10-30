# Deployment Checklist (D8)

Use this runbook for every production deployment. Complete the pre-deployment validation, then the
post-deployment validation, before declaring the release successful. See
[deployment-hardening.md](../deployment/deployment-hardening.md) for reverse-proxy, TLS, cookie and
Data Protection details.

---

## Pre-deployment validation

- [ ] CI is green (warnings-as-errors build + full test suite, including PostgreSQL parity when
      Docker is available).
- [ ] Release tag/artifact produced by the release job.
- [ ] Database backup taken (mandatory before destructive migrations).
- [ ] **Identity** migrations reviewed and ready to apply (expand/contract for rolling deployments).
- [ ] **CRM** migrations reviewed and ready to apply (`InitialCrm` on first deployment;
      expand/contract thereafter).
- [ ] **Sales** migrations reviewed and ready to apply (`InitialSales` on first deployment;
      expand/contract thereafter).
- [ ] `Identity:Jwt:SigningKeys` configured in the shared secret store for **all** instances.
- [ ] `ConnectionStrings:Identity` configured via secret.
- [ ] `ConnectionStrings:Crm` configured (or intentionally omitted to share the Identity database;
      CRM then uses the `crm` schema in that database).
- [ ] `ConnectionStrings:Sales` configured (or intentionally omitted to share the Identity database;
      Sales then uses the `sales` schema in that database).
- [ ] `AllowedHosts` set to the production host name(s), **not** `*` and not the development default.
- [ ] `DataProtection:ApplicationName` is identical on every instance.
- [ ] `DataProtection:KeysPath` points to a persistent, writable, shared directory. Outside
      Development the host refuses to start without it.
- [ ] The Data Protection directory is mounted on a volume that survives pod/container restarts and
      is not world-readable.
- [ ] `ReverseProxy:Enabled` is correct for the topology.
- [ ] `ReverseProxy:KnownProxies`/`KnownNetworks` list the trusted proxy hop(s). Leave both empty
      only for a same-host sidecar or direct deployment.
- [ ] `ReverseProxy:ForwardLimit` matches the number of trusted proxy hops.
- [ ] The proxy sends `X-Forwarded-Proto`, `X-Forwarded-Host`, `X-Forwarded-For` and preserves the
      original `Host` header.
- [ ] The proxy supports WebSocket upgrades and a long read timeout for the Blazor circuit.
- [ ] TLS terminates in front of the host; HSTS and HTTPS redirection are enabled.
- [ ] `Web:Cookies:SecurePolicy` is `Always` (the default outside Development). Development uses
      `SameAsRequest` only when the cookie section is absent.
- [ ] Identity database flags set correctly for the topology:
      `Identity:Database:ApplyMigrationsOnStartup=false` (HA) or `true` (single instance);
      `Identity:Database:VerifySchema=true`.
- [ ] `Crm:Database:VerifySchema=true` where the deployment owns the `crm` schema (recommended).
- [ ] `Sales:Database:VerifySchema=true` where the deployment owns the `sales` schema (recommended).
- [ ] `Identity:Permissions:SynchronizeOnStartup` set for the topology (`true` seeds the system
      roles from module catalogs; requires the Identity schema to be present).
- [ ] Liveness/readiness probe paths configured in the orchestrator.
- [ ] The previous artifact is retained for rollback.

---

## During deployment

- [ ] Apply **Identity** migrations as a gated step (before rolling out the new application version).
- [ ] Apply **CRM** migrations as a gated step (before rolling out the new application version).
- [ ] Apply **Sales** migrations as a gated step (before rolling out the new application version).
- [ ] Roll out instances gradually; wait for `/health/ready` to be green before continuing.
- [ ] Do not delete or replace the Data Protection key directory during the rollout.
- [ ] Monitor error rates, 401/403 rates, 409 concurrency conflicts and latency during rollout.

---

## Health verification

- [ ] `/health/live` returns `200` on every instance.
- [ ] `/health/ready` returns `200` and includes:
      - `identity-database`
      - `identity-authentication`
      - `data-protection`
      - `crm-database` and `sales-database`
      - the schema checks when enabled.
- [ ] The readiness probe is wired through the proxy using the public hostname.
- [ ] A deliberately unavailable database or invalid key ring fails readiness (do not route traffic
      to a degraded instance).

---

## Authentication verification

- [ ] Sign in through the reverse proxy with a real account.
- [ ] The browser receives `BSX.Web.Auth`, `BSX.Web.Refresh` and `BSX.Web.Antiforgery` cookies with
      `Secure`, `HttpOnly` and the expected `SameSite` values.
- [ ] The response includes HSTS and the configured security headers.
- [ ] The browser WebSocket circuit connects and the Blazor UI becomes interactive (no
      `blazor.server.js` or WebSocket errors in the console).
- [ ] Refresh the page after the access-token refresh threshold and confirm the session continues.
- [ ] Sign out and confirm the cookies are cleared and the refresh token is rejected.
- [ ] Revoke a session from the administration UI and confirm the affected session is rejected.
- [ ] Exercise one authorized and one unauthorized API call.
- [ ] Confirm no redirect loop occurs on HTTP or HTTPS.
- [ ] If multiple instances are used, sign in through one instance and confirm the cookie is accepted
      by another.
- [ ] Restart one instance and confirm an existing session still works (Data Protection
      persistence).

---

## Application smoke tests

- [ ] Smoke test Identity: login, refresh, list sessions, logout.
- [ ] Smoke test CRM: create a customer, read it back, list it with search, update it, and exercise
      one unauthorized call.
- [ ] Smoke test Sales: create a lead, contact and qualify it, create an opportunity, and exercise
      one unauthorized call. Verify that a lead referencing an unknown CRM customer returns `409`.
- [ ] Verify password-reset behaviour matches the deployment (delivery configured → `200`; not
      configured → `503`).
- [ ] Confirm no unexpected configuration warnings/errors in logs.

---

## Rollback guidance

- [ ] Keep the previous artifact available for immediate rollback.
- [ ] Ensure the previous version is compatible with the current schema (expand/contract), or
      restore the pre-migration backup.
- [ ] If a CRM or Sales migration must be rolled back, apply the migration bundle/`database update`
      to the previous migration and restore data only if the rollback is destructive.
- [ ] Keep the Data Protection key directory intact across rollbacks; deleting it signs every user
      out and invalidates antiforgery tokens.
- [ ] Roll back configuration changes that affect `AllowedHosts`, trusted proxies or cookie policy
      together with the application version.
- [ ] After rollback, re-run the health and authentication verification steps.
- [ ] Investigate and record the failure before retrying the deployment.
