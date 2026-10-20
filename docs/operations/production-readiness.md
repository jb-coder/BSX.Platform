# Production Readiness (D8)

Status of the Identity module after the hardening work on `feature/identity-hardening`.

## Findings from the previous Release Readiness Review

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

## Verification

- `dotnet build BSX.Platform.sln -c Release` → 0 warnings, 0 errors (warnings-as-errors).
- `dotnet test BSX.Platform.sln -c Release` → all tests green (unit, architecture, infrastructure
  integration, endpoint integration).

## Remaining known limitations (accepted / deferred)

- **Password-reset email delivery** requires the Notifications module; until then the endpoint
  returns `503` by design (safe and explicit).
- **Rate limiting** is per-instance; a distributed `IRateLimiter` is required for strict cluster-wide
  limits in HA.
- **PostgreSQL parity tests** (Testcontainers) are recommended before the first HA production
  rollout; endpoint/integration tests run on SQLite.
- **Integration events / Outbox emission** for cross-module communication (ADR-015 `BSX.Contracts`)
  is future work owned by downstream phases.
- **Service account / API key HTTP endpoints** and the **Blazor UI** are not part of this release.

None of the above block a single-instance production release. For HA, complete
[deployment-ha.md](deployment-ha.md) prerequisites (shared keys, distributed limiter).

## Statement

The Identity module is production-ready for a controlled release. All D1–D8 findings are resolved
or formally documented, security controls are fail-closed, and the build/test pipeline enforces
quality gates.
