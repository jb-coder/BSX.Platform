# Operations Guide (D8)

Day-2 runbook for the Identity module.

## Health & monitoring

| Signal | Where | Expected |
| --- | --- | --- |
| Liveness | `GET /health/live` | `200` while the process is up |
| Readiness | `GET /health/ready` | `200` when DB, auth (and schema) are healthy; `503` otherwise |
| Aggregate | `GET /health` | `200` / `503` |
| Logs | Serilog console (structured JSON-ready) | `Information+`; `Warning+` for `Microsoft.*` |
| Traces/metrics | OTLP when `OpenTelemetry:OtlpEndpoint` is set | exported to the collector |

## Key operational scenarios

### A user cannot log in

1. Check `401` responses for `Identity.Authentication.InvalidCredentials` — verify credentials.
2. Check for `429`/`409` `TooManyRequests` — rate limiting may be throttling the source.
3. Check for account lockout (repeated failures) — lockout releases automatically after the window.
4. Verify the account is `Active` and has a password credential.

### Tokens are rejected after a deployment

- Almost always a **signing-key mismatch**: an instance is using a different/ephemeral key. Confirm
  all instances share `Identity:Jwt:SigningKeys` ([jwt-key-management.md](jwt-key-management.md)).
- Check clock skew (> 30s) between issuer and validators.

### Refresh returns 409

- Concurrent refresh across instances. The client should retry once against the freshly persisted
  chain. The 30-second grace window tolerates legitimate concurrency.

### Password reset returns 503

- Expected when no delivery mechanism is configured (D3). Integrate the Notifications module or
  accept the documented behaviour ([password-reset-delivery.md](password-reset-delivery.md)).

### Readiness is red (`503`)

- Inspect the response body for the failing check:
  - `identity-database` → database unreachable (network/credentials).
  - `identity-authentication` → signing keys missing/unparsable.
  - `identity-database-schema` → pending migrations; apply them.

## Routine operations

- **Key rotation:** rolling add → propagate → wait > token TTL → remove
  ([jwt-key-management.md](jwt-key-management.md)).
- **Schema change:** apply migrations as a gated step before rollout
  ([database-migrations.md](database-migrations.md)).
- **Scaling:** ensure shared keys and a distributed rate limiter in HA
  ([deployment-ha.md](deployment-ha.md)).
- **Incident (suspected key compromise):** rotate keys immediately and revoke sessions /
  rotate security stamps.

## Backups & recovery

- Back up PostgreSQL (including `identity` schema and the outbox) on the platform's standard
  schedule.
- Test restore procedures; validate `/health/ready` after restore.
