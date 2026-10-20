# Health Checks (D6)

Identity exposes tagged health checks so orchestrators can distinguish process liveness from
service readiness.

## Endpoints

| Endpoint | Tag predicate | Checks | Use |
| --- | --- | --- | --- |
| `/health/live` | `live` | `self` | **Liveness** — restart the process if this fails |
| `/health/ready` | `ready` | `identity-database`, `identity-authentication`, and (opt-in) `identity-database-schema` | **Readiness** — route traffic only when green |
| `/health` | all | everything | Aggregate/convenience and legacy probes |

## Checks

| Name | Tag | Meaning |
| --- | --- | --- |
| `self` | `live` | The process is running and responding. No external dependency. |
| `identity-database` | `ready` | The identity database is reachable (`CanConnect`). |
| `identity-authentication` | `ready` | JWT signing keys are configured (key ring built). |
| `identity-database-schema` | `ready` | No pending EF Core migrations (opt-in via `Identity:Database:VerifySchema`). |

## Orchestrator wiring

- **Liveness probe** → `/health/live`. Never include dependencies here: a database blip must not
  cause a restart storm.
- **Readiness probe** → `/health/ready`. Remove the instance from load balancing while a dependency
  is down or the schema is out of date.

## Notes

- `/health/ready` returns `503 Service Unavailable` when any `ready` check is unhealthy.
- The schema check is enabled with `Identity:Database:VerifySchema=true`
  ([database-migrations.md](database-migrations.md)).
- A misconfigured host (for example an unparsable signing key) fails closed: it never reports ready.
