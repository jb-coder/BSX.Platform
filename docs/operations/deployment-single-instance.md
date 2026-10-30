# Single-Instance Deployment (D4)

Topology: one `BSX.Web` instance, one PostgreSQL database. Suitable for development, staging and
small production workloads.

## Topology

```
Client ──TLS──▶ BSX.Web (1 replica) ──▶ PostgreSQL
                     │
                     └──▶ auth: self-issued JWT (RS256)
```

## Requirements

- **Connection strings** via secrets: `ConnectionStrings__Identity` and (optionally)
  `ConnectionStrings__Crm`. Without `ConnectionStrings__Crm`, CRM shares the Identity database and
  uses its own `crm` schema.
- **JWT signing keys** via secret: `Identity__Jwt__SigningKeys__0__PrivateKeyPem` (and `...__Kid`).
  The host fails fast at startup if they are missing outside Development.
- **Data Protection** via `DataProtection__ApplicationName` and `DataProtection__KeysPath`. The path
  must be a persistent, writable directory so authentication cookies and antiforgery tokens survive
  restarts. The host fails fast outside Development if it is missing.
- **TLS** terminated at the host or a reverse proxy; `UseHttpsRedirection` and HSTS are active
  outside Development. If a proxy terminates TLS, configure `ReverseProxy:KnownProxies`/`KnownNetworks`
  and make sure the proxy forwards `X-Forwarded-Proto`, `X-Forwarded-Host` and `X-Forwarded-For`.
- **Migrations** applied before startup, or `Identity__Database__ApplyMigrationsOnStartup=true`
  for Identity (safe only with a single instance). **CRM and Sales migrations are always applied out
  of band**; there are no CRM/Sales startup-migration hooks.

## Characteristics

- **Rate limiting** is per-instance and fully effective (only one instance). See
  [rate-limiting.md](rate-limiting.md).
- **Health checks**: expose `/health/live` and `/health/ready` to the platform probe. Readiness
  includes both `identity-database` and `crm-database` (plus the optional schema checks). See
  [health-checks.md](health-checks.md).
- **CRM concurrency**: concurrent updates are rejected with `409 Conflict` (optimistic
  concurrency); clients re-read and retry. See [crm-operations.md](crm-operations.md).
- **Sessions/refresh tokens** are database-backed; no shared cache is required.
- **Password reset** requires the Notifications module (see
  [password-reset-delivery.md](password-reset-delivery.md)); otherwise the endpoint returns `503`.

## Recommended configuration

```json
{
  "Identity": {
    "Database": { "ApplyMigrationsOnStartup": true, "VerifySchema": true }
  },
  "Crm": {
    "Database": { "VerifySchema": true }
  },
  "Sales": {
    "Database": { "VerifySchema": true }
  },
  "DataProtection": {
    "ApplicationName": "BSX.Platform",
    "KeysPath": "/var/bsx/data-protection-keys"
  },
  "ReverseProxy": {
    "Enabled": true,
    "RedirectHttpToHttps": true,
    "ForwardLimit": 1,
    "KnownProxies": [ "127.0.0.1" ]
  },
  "AllowedHosts": "your.domain.example"
}
```
