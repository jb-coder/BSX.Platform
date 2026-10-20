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

- **Connection string** via secret: `ConnectionStrings__Identity`.
- **JWT signing keys** via secret: `Identity__Jwt__SigningKeys__0__PrivateKeyPem` (and `...__Kid`).
  The host fails fast at startup if they are missing outside Development.
- **TLS** terminated at the host or a reverse proxy; `UseHttpsRedirection` and HSTS are active
  outside Development.
- **Migrations** applied before startup, or `Identity__Database__ApplyMigrationsOnStartup=true`
  (safe only with a single instance).

## Characteristics

- **Rate limiting** is per-instance and fully effective (only one instance). See
  [rate-limiting.md](rate-limiting.md).
- **Health checks**: expose `/health/live` and `/health/ready` to the platform probe. See
  [health-checks.md](health-checks.md).
- **Sessions/refresh tokens** are database-backed; no shared cache is required.
- **Password reset** requires the Notifications module (see
  [password-reset-delivery.md](password-reset-delivery.md)); otherwise the endpoint returns `503`.

## Recommended configuration

```json
{
  "Identity": {
    "Database": { "ApplyMigrationsOnStartup": true, "VerifySchema": true }
  },
  "AllowedHosts": "your.domain.example"
}
```
