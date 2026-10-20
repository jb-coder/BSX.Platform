# Security Checklist (D8)

Review before every production release. Each item should have an owner and evidence.

## Secrets & configuration

- [ ] `Identity:Jwt:SigningKeys` are provided from a secret store and are **shared** across
      instances (no ephemeral keys).
- [ ] `ConnectionStrings:Identity` is provided from a secret store; no default credentials.
- [ ] `AllowedHosts` is set to the production host name(s); not `*`.
- [ ] No secrets are committed to source control; no secrets appear in logs.

## Authentication & tokens

- [ ] Access-token lifetime is short (default 15 minutes) and appropriate for the risk profile.
- [ ] Signing keys are rotated with an overlap window (see jwt-key-management.md).
- [ ] Clock synchronisation (NTP) is in place for issuer and validators.
- [ ] Refresh tokens are transmitted only over TLS.

## Authorization & tenancy

- [ ] Permission checks are enforced (fail-closed); new endpoints declare `[RequirePermission]`.
- [ ] Tenant-scoped endpoints resolve the tenant from the token and enforce `TenantGuard`.
- [ ] Privileged endpoints require explicit permissions (no implicit admin).

## Password reset

- [ ] Delivery is either configured (Notifications module) or the endpoint returns `503`
      (never a silent success).
- [ ] Reset tokens are single-use, hashed at rest, and short-lived (30 minutes).
- [ ] Reset completion revokes all sessions.

## Transport & host

- [ ] TLS terminates in front of the host; HSTS and HTTPS redirection are enabled.
- [ ] Host-header filtering is effective (`AllowedHosts`).
- [ ] Antiforgery is configured appropriately for browser vs API clients.

## Data

- [ ] Migrations are applied via the gated deployment step; a backup exists before destructive
      changes.
- [ ] Auditing and soft-delete are active (ADR-010).

## Observability & operations

- [ ] Liveness (`/health/live`) and readiness (`/health/ready`) probes are wired.
- [ ] Structured logs and (optionally) OTLP traces/metrics are exported.
- [ ] Rate limiting is in place; a distributed limiter is used in HA.

## Dependency & build hygiene

- [ ] CI is green: warnings-as-errors build and the full test suite pass.
- [ ] Package versions are centralised in `Directory.Packages.props`.
