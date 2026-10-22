# Health Checks (D6)

The host exposes tagged health checks so orchestrators can distinguish process liveness from
service readiness. Identity and CRM contribute checks to the same endpoints.

## Endpoints

| Endpoint | Tag predicate | Checks | Use |
| --- | --- | --- | --- |
| `/health/live` | `live` | `self` | **Liveness** — restart the process if this fails |
| `/health/ready` | `ready` | `identity-database`, `identity-authentication`, `crm-database`, and (opt-in) `identity-database-schema` / `crm-database-schema` | **Readiness** — route traffic only when green |
| `/health` | all | everything | Aggregate/convenience and legacy probes |

## Checks

| Name | Tag | Meaning |
| --- | --- | --- |
| `self` | `live` | The process is running and responding. No external dependency. |
| `identity-database` | `ready` | The identity database is reachable (`CanConnect`). |
| `identity-authentication` | `ready` | JWT signing keys are configured (key ring built). |
| `identity-database-schema` | `ready` | No pending Identity EF Core migrations (opt-in via `Identity:Database:VerifySchema`). |
| `crm-database` | `ready` | The CRM database is reachable (`CanConnect`). |
| `crm-database-schema` | `ready` | No pending CRM EF Core migrations (opt-in via `Crm:Database:VerifySchema`). |

When CRM shares the Identity database, both database checks still run: they validate the same
connection from each module's persistence context.

## Orchestrator wiring

- **Liveness probe** → `/health/live`. Never include dependencies here: a database blip must not
  cause a restart storm.
- **Readiness probe** → `/health/ready`. Remove the instance from load balancing while a dependency
  is down or either schema is out of date.

## Notes

- `/health/ready` returns `503 Service Unavailable` when any `ready` check is unhealthy.
- Schema checks are enabled with `Identity:Database:VerifySchema=true` and
  `Crm:Database:VerifySchema=true` ([database-migrations.md](database-migrations.md)).
- A misconfigured host (for example an unparsable signing key) fails closed: it never reports ready.
- Enabling a schema check on a database created with `EnsureCreated` (integration tests) reports
  pending migrations by design; production schemas must be migration-managed.
