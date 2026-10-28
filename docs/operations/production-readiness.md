# Production Readiness (D8)

Status of the Identity, CRM and Sales modules after the Identity hardening
(`feature/identity-hardening`), CRM release hardening (`feature/crm-hardening`) and Sales API
(`feature/sales-api`) work.

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

## Sales API release gates

The Sales Infrastructure review approved the layer with minor notes that were documented as
production gates. They are resolved before API exposure.

| ID | Gate | Severity | Status | Resolution |
| --- | --- | --- | --- | --- |
| S1 | Temporary pass-through CRM reference validation | High | **Resolved** | `CRM.Contracts.ICrmReferenceLookup` + `CrmReferenceLookup` (CRM Infrastructure); `CrmContractReferenceValidator`; fail-closed fallback when CRM is absent. No Sales → CRM Domain reference. |
| S2 | `CrmReferenceValidationException` surfaced as HTTP 500 | High | **Resolved** | `SalesCrmReferenceExceptionFilter` maps it to `409 Conflict` with `SALES.Customer.NotFound` / `SALES.Organization.NotFound`; endpoint integration tests cover missing, cross-tenant and invalid references. |
| S3 | Sales permissions not seedable by Identity | Medium | **Resolved** | `SalesPermissionCatalog` (`IPermissionCatalogSource`); Identity `SystemRolePermissionSynchronizer` seeds `Administrator`/`Auditor`; unit/integration tests verify catalog and seeding. |
| S4 | Concurrency conflict mapping | Medium | **Resolved** | `SalesConcurrencyConflictFilter` maps `ConcurrencyConflictException` to `409 SALES.ConcurrencyConflict`. |
| S5 | Sales health checks | Medium | **Resolved** | `sales-database` and opt-in `sales-database-schema` readiness checks. |
| S6 | API surface | High | **Resolved** | `BSX.Modules.Sales.Endpoints` exposes the lead and opportunity endpoints under `/sales`. |

## Platform hardening (v0.4.0)

The platform foundation was hardened for the next business modules. No new business module was
introduced.

| Objective | Outcome |
| --- | --- |
| Outbox durability (ADR-009) | `BSX.Persistence` transactional outbox in Identity, CRM and Sales; atomic persistence, atomic claim (exactly-once per attempt), handler isolation, exponential retry, dead-letter, stale-claim recovery and background recovery loop. |
| Permission catalogs (ADR-018) | Platform `PermissionCatalog`; Identity, CRM and Sales publish catalogs; system roles are reconciled declaratively, including **removal of stale permissions**; conflicts fail fast. |
| Architecture governance | Domain/Contracts/Application/Endpoints/Infrastructure dependency tests, module isolation, platform assembly isolation and a test that every `[RequirePermission]` is declared in a catalog. |
| Observability | `BSX.Platform` meter/activity source with domain-event, outbox and permission-sync counters/histograms/backlog; OTLP wiring. See [observability.md](observability.md). |
| Rate limiting | Platform `IRateLimitStore` / `IDistributedRateLimitStore` abstraction with a single-instance default and documented HA path. See [rate-limiting.md](rate-limiting.md). |

Verification: Release build with warnings-as-errors; full unit, architecture, infrastructure,
PostgreSQL parity (when Docker is available) and endpoint integration suites green.

## Verification

- `dotnet build BSX.Platform.sln -c Release` → 0 warnings, 0 errors (warnings-as-errors).
- `dotnet test BSX.Platform.sln -c Release` → all tests green (unit, architecture, infrastructure
  integration, endpoint integration, composition).
- PostgreSQL parity tests run when Docker is available (GitHub-hosted runners provide it); locally
  they are reported as skipped with an explanatory message.
- Sales endpoint integration tests exercise leads, opportunities, security, CRM validation,
  concurrency, health and the three-module composite unit of work.

## Remaining known limitations (accepted / deferred)

- **Password-reset email delivery** requires the Notifications module; until then the endpoint
  returns `503` by design (safe and explicit).
- **Rate limiting** is per-instance by default; implement `IDistributedRateLimitStore` (for example
  Redis) for strict cluster-wide limits in HA. CRM and Sales business endpoints are
  authenticated/permissioned but not individually throttled.
- **CRM search** uses lower-cased `LIKE` columns. It is portable and case-insensitive, but a
  leading-wildcard search cannot use the B-tree indexes; introduce `pg_trgm`/prefix search if data
  volume requires it.
- **Cross-module atomicity.** The composite unit of work commits each module's transaction
  separately. Keep commands module-scoped; use outbox/saga for future cross-module workflows.
- **CRM and Sales optimistic concurrency** is server-side (no `ETag`/`If-Match`); clients re-read
  on `409`.
- **Integration event publishing** is fully persisted but remains dormant until a broker-backed
  `IEventBus` is registered and `Outbox:IntegrationPublishingEnabled` is turned on.
- **Service account / API key HTTP endpoints** and the **Sales business screens** are not part of
  this release. Web authentication, workspace selection, the shell, customers, organizations,
  organization assignment, contacts and addresses are implemented
  ([foundation.md](../web/foundation.md), [crm.md](../web/crm.md)).

None of the above block a single-instance production release. For HA, complete
[deployment-ha.md](deployment-ha.md) prerequisites (shared keys, distributed limiter).

## Statement

The Identity, CRM and Sales modules are production-ready for a controlled release. All Identity D1-D8, CRM C1-C4 and Sales S1-S6 findings are resolved or formally documented, security controls are fail-closed, tenant isolation is fail-closed, and the build/test pipeline enforces quality gates on the production provider.
