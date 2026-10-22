# Production Readiness (D8)

Status of the Identity and CRM modules after the Identity hardening
(`feature/identity-hardening`) and CRM release hardening (`feature/crm-hardening`) work.

## Identity findings

| ID | Finding | Status | Resolution |
| --- | --- | --- | --- |
| D1 | Ephemeral JWT signing keys | **Resolved** | `JwtOptionsConfiguration` permits ephemeral keys only in Development and fails fast otherwise; startup validation added; rotation documented; tests added. |
| D2 | No migration strategy | **Resolved** | Official out-of-band strategy documented; optional startup migration; schema readiness check (`identity-database-schema`) detects drift; tests added. |
| D3 | Password reset is a silent no-op | **Resolved** | Delivery availability is explicit; endpoint returns `503` when unconfigured; integration seam for Notifications documented; tests added. |
| D4 | Deployment topology undocumented | **Resolved** | Single-instance, HA, rate-limiting and authentication deployment guides added. |
| D5 | No CI pipeline | **Resolved** | `.github/workflows/ci.yml` with build/test/release jobs (warnings fail builds); pipeline documentation added. |
| D6 | Health strategy not split | **Resolved** | Tagged liveness (`/health/live`) and readiness (`/health/ready`) checks; tests added. |
| D7 | Host config not hardened | **Resolved** | Fail-fast connection string outside Development; `AllowedHosts` restricted by default; JWT validation; configuration reference added. |
| D8 | Missing security/ops docs | **Resolved** | Security checklist, deployment checklist, operations guide and this document added. |

## CRM release-hardening findings

The CRM Release Readiness Review returned **Approved for Release with Minor Notes**. The notes are
resolved below.

| ID | Finding | Classification | Status | Resolution |
| --- | --- | --- | --- | --- |
| C1 | `ConcurrencyConflictException` surfaced as HTTP 500 | High | **Resolved** | `CrmConcurrencyConflictFilter` maps it to `409 Conflict` with the standard RFC 9457 problem body (`CRM.ConcurrencyConflict`); endpoint and infrastructure concurrency tests added. |
| C2 | No PostgreSQL parity coverage | Medium | **Resolved** | Testcontainers PostgreSQL suite validates migrations, schema, filtered unique index, tenant filters, tenant-key check constraint, search, soft delete and optimistic concurrency; tests skip cleanly when Docker is unavailable. |
| C3 | CRM missing from deployment/operations docs | Medium | **Resolved** | Migration commands, deployment checklist, health checks, configuration and a dedicated [CRM operations guide](crm-operations.md) added. |
| C4 | Optional readiness/tooling improvements | Low | **Resolved** | Optional `crm-database-schema` readiness check added; `dotnet-ef` pinned to the package version via `.config/dotnet-tools.json`; design-time factories accept the documented `--connection` argument. |

## Verification

- `dotnet build BSX.Platform.sln -c Release` → 0 warnings, 0 errors (warnings-as-errors).
- `dotnet test BSX.Platform.sln -c Release` → all tests green (unit, architecture, infrastructure
  integration, endpoint integration, composition).
- PostgreSQL parity tests run when Docker is available (GitHub-hosted runners provide it); locally
  they are reported as skipped with an explanatory message.

## Remaining known limitations (accepted / deferred)

- **Password-reset email delivery** requires the Notifications module; until then the endpoint
  returns `503` by design (safe and explicit).
- **Rate limiting** is per-instance; a distributed `IRateLimiter` is required for strict cluster-wide
  limits in HA. CRM business endpoints are authenticated/permissioned but not individually throttled.
- **CRM search** uses lower-cased `LIKE` columns. It is portable and case-insensitive, but a
  leading-wildcard search cannot use the B-tree indexes; introduce `pg_trgm`/prefix search if data
  volume requires it.
- **Cross-module atomicity.** The composite unit of work commits each module's transaction
  separately. Keep commands module-scoped; use outbox/saga for future cross-module workflows.
- **CRM optimistic concurrency** is server-side (no `ETag`/`If-Match`); clients re-read on `409`.
- **Integration events / Outbox emission** for cross-module communication (ADR-015 `BSX.Contracts`)
  is future work owned by downstream phases.
- **Service account / API key HTTP endpoints** and the **Blazor UI** are not part of this release.

None of the above block a single-instance production release. For HA, complete
[deployment-ha.md](deployment-ha.md) prerequisites (shared keys, distributed limiter).

## Statement

The Identity and CRM modules are production-ready for a controlled release. All Identity D1–D8 and
CRM C1–C4 findings are resolved or formally documented, security controls are fail-closed, tenant
isolation is fail-closed, and the build/test pipeline enforces quality gates on the production
provider.
