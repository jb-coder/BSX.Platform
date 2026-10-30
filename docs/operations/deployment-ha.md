# High-Availability Deployment (D4)

Topology: multiple `BSX.Web` instances behind a load balancer, one shared PostgreSQL database.

## Topology

```
                    ┌────────────┐
Clients ──TLS──▶ LB │ BSX.Web xN │──▶ PostgreSQL (primary)
                    └────────────┘
                          │
                          └──▶ shared secret store (JWT keys)
```

## Hard requirements

1. **Shared JWT signing keys.** Every instance must load the *same* `Identity:Jwt:SigningKeys`
   from a shared secret store. An instance with a different or ephemeral key would reject tokens
   issued by its peers. See [jwt-key-management.md](jwt-key-management.md).
2. **Shared Data Protection key ring.** Every instance must use the same
   `DataProtection:ApplicationName` and a shared, persistent `DataProtection:KeysPath`. Without it,
   authentication cookies and antiforgery tokens issued by one instance are unreadable by its peers
   and are lost on restart. The host fails fast outside Development when the path is missing.
3. **Forwarded headers.** The load balancer/reverse proxy must send `X-Forwarded-Proto`,
   `X-Forwarded-Host` and `X-Forwarded-For`, and every instance must trust the proxy hop via
   `ReverseProxy:KnownProxies`/`KnownNetworks`. Set `ReverseProxy:ForwardLimit` to the number of
   trusted proxy hops.
4. **Migrations applied out-of-band.** Do **not** set `ApplyMigrationsOnStartup=true`. Apply
   **Identity, CRM and Sales** migrations as gated deployment steps before rolling out the new
   version. See [database-migrations.md](database-migrations.md).
5. **Sticky independence.** Access tokens are stateless and validated locally; refresh tokens and
   sessions are in PostgreSQL, so no session affinity is required. Any instance can serve any
   request. Workspace selection is the exception: `PendingLoginStore` is in-process for five
   minutes, so use session affinity or a distributed replacement for that window.
6. **Health-gated rollout.** Wire `/health/live` (liveness) and `/health/ready` (readiness) so the
   orchestrator only routes to instances whose databases, schemas, JWT keys and Data Protection key
   ring are ready.
7. **CRM and Sales are stateless.** Their state lives in PostgreSQL; any instance can serve any
   request. A concurrent update is rejected with `409 Conflict` and the client re-reads/retries
   ([crm-operations.md](crm-operations.md#concurrency)).

## Rate limiting in HA

The default rate limiter is **in-process** — limits apply per instance and are not shared. With N
instances the effective limit is approximately N× the configured value. For strict, cluster-wide
limits, provide a distributed `IRateLimiter` implementation (for example a Redis-backed one) and
register it in place of the default. See [rate-limiting.md](rate-limiting.md).

## Session/refresh behaviour

- Refresh rotation uses optimistic concurrency; a concurrent refresh on another instance can return
  `409 Conflict` and the client should retry against the freshly persisted chain.
- The 30-second grace window (ADR-017) tolerates legitimate concurrent refreshes across instances.

## Recommended configuration

```json
{
  "Identity": {
    "Database": { "ApplyMigrationsOnStartup": false, "VerifySchema": true }
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
    "KnownNetworks": [ "10.20.0.0/24" ]
  },
  "AllowedHosts": "your.domain.example"
}
```

The `DataProtection:KeysPath` directory must be a shared persistent volume, not a per-pod empty
directory. See [deployment-hardening.md](../deployment/deployment-hardening.md).
