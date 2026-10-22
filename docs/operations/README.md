# Operations

Operational guidance for running the BSX Platform and its Identity, CRM and Sales modules in
production.

| Document | Purpose |
| --- | --- |
| [jwt-key-management.md](jwt-key-management.md) | Signing key configuration and rotation (D1) |
| [database-migrations.md](database-migrations.md) | Identity and CRM schema strategy and readiness (D2) |
| [crm-operations.md](crm-operations.md) | CRM deployment, tenancy, concurrency, search and troubleshooting |
| [password-reset-delivery.md](password-reset-delivery.md) | Reset delivery and the Notifications seam (D3) |
| [deployment-single-instance.md](deployment-single-instance.md) | Single-instance topology (D4) |
| [deployment-ha.md](deployment-ha.md) | High-availability topology (D4) |
| [rate-limiting.md](rate-limiting.md) | Rate limiter behaviour and scaling (D4) |
| [authentication-deployment.md](authentication-deployment.md) | Authentication deployment guidance (D4) |
| [health-checks.md](health-checks.md) | Liveness and readiness probes (D6) |
| [observability.md](observability.md) | Logs, traces, metrics, outbox backlog and alerts |
| [configuration.md](configuration.md) | Configuration keys and validation (D7) |
| [ci-cd.md](ci-cd.md) | Build, test and release pipeline (D5) |
| [security-checklist.md](security-checklist.md) | Security review checklist (D8) |
| [deployment-checklist.md](deployment-checklist.md) | Pre- and post-deployment checklist (D8) |
| [operations-guide.md](operations-guide.md) | Day-2 operations runbook (D8) |
| [production-readiness.md](production-readiness.md) | Production readiness statement (D8) |

## Principles

- **Fail fast.** Misconfiguration outside Development stops the host at startup rather than
  degrading silently.
- **Fail closed.** Missing authentication, permissions or tenant context deny access by default.
- **Explicit over implicit.** Capabilities that are not available (for example password-reset
  delivery) report an explicit error instead of a misleading success.
