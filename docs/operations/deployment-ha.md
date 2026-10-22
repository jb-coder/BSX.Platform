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
2. **Migrations applied out-of-band.** Do **not** set `ApplyMigrationsOnStartup=true`. Apply
   **both Identity and CRM** migrations as gated deployment steps before rolling out the new
   version. CRM has no startup-migration hook. See
   [database-migrations.md](database-migrations.md).
3. **Sticky independence.** Access tokens are stateless and validated locally; refresh tokens and
   sessions are in PostgreSQL, so no session affinity is required. Any instance can serve any
   request.
4. **Health-gated rollout.** Wire `/health/live` (liveness) and `/health/ready` (readiness) so the
   orchestrator only routes to instances whose Identity and CRM databases and (optionally) schemas
   are ready.
5. **CRM is stateless.** CRM state lives in PostgreSQL; any instance can serve any request. A
   concurrent update is rejected with `409 Conflict` and the client re-reads/retries
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
  "AllowedHosts": "your.domain.example"
}
```
