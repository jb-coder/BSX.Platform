# Operations Guide (D8)

Day-2 runbook for the BSX Platform (Identity and CRM modules).

## Health & monitoring

| Signal | Where | Expected |
| --- | --- | --- |
| Liveness | `GET /health/live` | `200` while the process is up |
| Readiness | `GET /health/ready` | `200` when both databases, authentication and (opt-in) schemas are healthy; `503` otherwise |
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

### CRM update returns 409

- Expected optimistic-concurrency behaviour (`CRM.ConcurrencyConflict`): another request saved the
  same customer/organization first. The stale write was rejected; re-read the resource and retry.
- CRM does not use ETags; the version is server-side. See
  [crm-operations.md](crm-operations.md#concurrency).

### CRM reads return no data

1. Confirm the token carries a tenant (`ITenantContext`); a request without a tenant is
   **fail-closed** and sees no CRM rows.
2. Confirm `crm-database` is ready. A missing/wrong tenant is not an error; it is an empty result.
3. Cross-tenant detail reads intentionally return `404`.

### Password reset returns 503

- Expected when no delivery mechanism is configured (D3). Integrate the Notifications module or
  accept the documented behaviour ([password-reset-delivery.md](password-reset-delivery.md)).

### Readiness is red (`503`)

- Inspect the response body for the failing check:
  - `identity-database` → identity database unreachable (network/credentials).
  - `identity-authentication` → signing keys missing/unparsable.
  - `identity-database-schema` → pending Identity migrations; apply them.
  - `crm-database` → CRM database (or shared database) unreachable.
  - `crm-database-schema` → pending CRM migrations; apply them.

## Routine operations

- **Key rotation:** rolling add → propagate → wait > token TTL → remove
  ([jwt-key-management.md](jwt-key-management.md)).
- **Schema change:** apply Identity and CRM migrations as gated steps before rollout
  ([database-migrations.md](database-migrations.md)).
- **Scaling:** ensure shared keys and a distributed rate limiter in HA
  ([deployment-ha.md](deployment-ha.md)).
- **CRM tenant operations:** background/operations code may call `CrmTenantScope.UseTenant` or
  `UseSystemScope`; request paths must never do so
  ([crm-operations.md](crm-operations.md#tenant-scope)).
- **Incident (suspected key compromise):** rotate keys immediately and revoke sessions /
  rotate security stamps.

## Backups & recovery

- Back up PostgreSQL (including the `identity` and `crm` schemas) on the platform's standard
  schedule.
- Test restore procedures; validate `/health/ready` after restore.
