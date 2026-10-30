# Health Checks (D6)

The host exposes tagged health checks so orchestrators can distinguish process liveness from
service readiness. Identity, CRM and Sales contribute checks to the same endpoints.

## Endpoints

| Endpoint | Tag predicate | Checks | Use |
| --- | --- | --- | --- |
| `/health/live` | `live` | `self` | **Liveness** — restart the process if this fails |
| `/health/ready` | `ready` | `identity-database`, `identity-authentication`, `data-protection`, `crm-database`, `sales-database`, and (opt-in) `identity-database-schema` / `crm-database-schema` / `sales-database-schema` | **Readiness** — route traffic only when green |
| `/health` | all | everything | Aggregate/convenience and legacy probes |

## Checks

| Name | Tag | Meaning |
| --- | --- | --- |
| `self` | `live` | The process is running and responding. No external dependency. |
| `identity-database` | `ready` | The identity database is reachable (`CanConnect`). |
| `identity-authentication` | `ready` | JWT signing keys are configured (key ring built). |
| `data-protection` | `ready` | The Data Protection key ring can protect and unprotect a payload (cookie and antiforgery dependency). |
| `identity-database-schema` | `ready` | No pending Identity EF Core migrations (opt-in via `Identity:Database:VerifySchema`). |
| `crm-database` | `ready` | The CRM database is reachable (`CanConnect`). |
| `crm-database-schema` | `ready` | No pending CRM EF Core migrations (opt-in via `Crm:Database:VerifySchema`). |
| `sales-database` | `ready` | The Sales database is reachable (`CanConnect`). |
| `sales-database-schema` | `ready` | No pending Sales EF Core migrations (opt-in via `Sales:Database:VerifySchema`). |

When CRM and/or Sales share the Identity database, all database checks still run: they validate the
same connection from each module's persistence context.

## Orchestrator wiring

- **Liveness probe** → `/health/live`. Never include dependencies here: a database blip must not
  cause a restart storm.
- **Readiness probe** → `/health/ready`. Remove the instance from load balancing while a dependency
  is down or either schema is out of date.

## Notes

- `/health/ready` returns `503 Service Unavailable` when any `ready` check is unhealthy.
- Schema checks are enabled with `Identity:Database:VerifySchema=true`,
  `Crm:Database:VerifySchema=true` and `Sales:Database:VerifySchema=true`
  ([database-migrations.md](database-migrations.md)).
- A misconfigured host (for example an unparsable signing key) fails closed: it never reports ready.
- The `data-protection` check fails when the key ring cannot protect or unprotect a payload, so an
  instance with a missing or unwritable key directory is removed from rotation.
- Enabling a schema check on a database created with `EnsureCreated` (integration tests) reports
  pending migrations by design; production schemas must be migration-managed.
