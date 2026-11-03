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

## Identity provider parity

The Identity audit found that the module had no PostgreSQL integration coverage. The behavior was
not known to be broken, but it had not been proven against the production provider.

| ID | Finding | Severity | Status | Resolution |
| --- | --- | --- | --- | --- |
| I1 | Identity had no PostgreSQL integration coverage | Medium | **Resolved** | Testcontainers PostgreSQL parity suite (`PostgreSqlMigrationTests`, `PostgreSqlParityTests`, `PostgreSqlSecurityParityTests`) validates the full migration chain, schema, unique indexes, cascading foreign keys, concurrency tokens, transactions/outbox, user/role/membership/session persistence, refresh-token rotation and revocation, lockout hardening (ADR-022), permission scope boundaries (ADR-020) and authorization revalidation dependencies (ADR-021). See [postgresql-parity.md](../testing/postgresql-parity.md). |

## Pilot readiness hardening

| ID | Finding | Severity | Status | Resolution |
| --- | --- | --- | --- | --- |
| P1 | `AllowedHosts` validation accepted a wildcard entry inside a semicolon-separated list | High | **Resolved** | The startup check now rejects any wildcard entry and a loopback-only development list; regression tests cover `*`, `example.com;*`, `*;example.com`, whitespace and `*.example.com`. |
| P2 | Pending workspace selection retained the password and placed the bearer token in the URL | Medium | **Resolved** | The pending record contains the verified user id and tenant options only. The URL carries a non-secret selection handle; the single-use secret is held in the `BSX.Web.PendingLogin` HttpOnly cookie. `CompletePendingLoginCommand` consumes the proof, validates the handle, tenant, user and membership, and issues the session. |
| P3 | `InvitedByUserId` was caller-controlled | Medium | **Resolved** | Invitation provenance is derived from `ICurrentUser`; the legacy wire field is ignored. Regression tests prove a spoofed value is not persisted. |
| P4 | Service account and API key commands had no permission declaration; API key creation had no tenant guard | Medium (latent) | **Resolved** | New tenant-scoped permissions `identity.service-accounts.create`, `identity.api-keys.create` and `identity.api-keys.revoke`; all related commands require them; API key creation and revocation validate the service account tenant. |
| P5 | Documentation described pre-ADR-021 security-stamp semantics and additive-only permission synchronization | Low | **Resolved** | Authentication, JWT, permissions, migration and deployment documents now describe per-operation revalidation and declarative reconciliation. |

## Post-hardening Identity closures

| ID | Finding | Status | Resolution |
| --- | --- | --- | --- |
| H1 | Membership removal left tenant refresh sessions usable | **Resolved** | Membership removal revokes active sessions bound to that tenant in the same unit of work; refresh rejects missing/inactive memberships before rotation and revokes the session on failure. |
| H2 | Cross-tenant role metadata read by role id | **Resolved** | `GetRoleByIdQuery` applies the tenant guard to tenant roles; platform/system roles remain shared read-only metadata. Role detail/edit/permissions web screens now reject another tenant's role. |
| M1 | Membership acceptance had no tenant binding | **Resolved** | `AcceptMembershipCommandHandler` applies `TenantGuard` before accepting; cross-tenant acceptance is rejected. |
| M2 | API key revocation had no authorization or tenant binding | **Resolved** | `RevokeApiKeyCommand` requires `identity.api-keys.revoke` and validates the owning service account's tenant. |
| M3 | Pending workspace chooser exposed PII without proof of possession | **Resolved** | The chooser is static SSR and requires the `BSX.Web.PendingLogin` cookie before rendering the email or tenant options; the non-secret URL handle alone is insufficient. |
| M4 | Removed memberships could not be re-invited | **Resolved** | Re-invitation reuses the existing membership row, returns it to `Invited`, clears prior roles and requires fresh acceptance. |

## Deployment hardening

The deployment audit identified reverse-proxy, cookie and Data Protection concerns. They are
resolved below.

| ID | Finding | Severity | Status | Resolution |
| --- | --- | --- | --- | --- |
| DP1 | Reverse proxy topology not proven | High | **Resolved** | Forwarded-header processing (`X-Forwarded-Proto`, `X-Forwarded-Host`, `X-Forwarded-For`) runs before HTTPS redirection, HSTS and authentication; trust is limited to configured proxies/networks. Covered by `DeploymentHardeningTests`. |
| DP2 | Forwarded headers missing/unverified | High | **Resolved** | `ReverseProxyOptions` with `KnownProxies`, `KnownNetworks`, `ForwardLimit` and a loopback-only default; untrusted-proxy spoofing is tested. |
| DP3 | Cookies behind TLS termination | High | **Resolved** | `Web:Cookies:SecurePolicy` defaults to `Always` outside Development; auth/refresh/antiforgery/pending-selection cookies are `HttpOnly` with explicit `SameSite`; forwarded HTTPS issues `Secure` cookies. |
| DP4 | Data Protection persistence unvalidated | High | **Resolved** | Persistent, shared key ring via `DataProtection:KeysPath`; identical `ApplicationName`; startup fails fast when the path is missing/unwritable; a `data-protection` readiness check and a persistence test prove restart behavior. See [deployment-hardening.md](../deployment/deployment-hardening.md). |
| DP5 | Production deployment assumptions unverified | Medium | **Resolved** | `AllowedHosts` rejects any wildcard entry in the semicolon-separated list and the loopback-only development default outside Development; security headers and HSTS are emitted; the pre/post-deployment runbook covers health, authentication, cookies and rollback. |

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
- **Service account / API key HTTP endpoints** are not part of this release. The underlying
  `CreateServiceAccountCommand` and `CreateApiKeyCommand` are permission-guarded and tenant-guarded,
  so exposing them later does not create an unguarded write path. Web authentication, workspace
  selection, the shell, CRM, Sales and Administration workflows are implemented
  ([foundation.md](../web/foundation.md), [crm.md](../web/crm.md), [sales.md](../web/sales.md),
  [administration.md](../web/administration.md)).

None of the above block a single-instance production release. For HA, complete
[deployment-ha.md](deployment-ha.md) prerequisites (shared keys, distributed limiter).

## Demo readiness

A development-only, opt-in demo workspace is available for customer demonstrations and pilot
onboarding. `Demo:SeedIdentity` and `Demo:SeedSampleData` create an administrator, sample CRM and
Sales records, and active users that are ready to invite. The seeders are idempotent and use the
normal application commands. They are ignored outside Development and are not part of a production
deployment. See [demo readiness](../demo/demo-readiness.md).

The authenticated home experience, first-run checklist and contextual empty states cover the
customer → lead → opportunity → invitation journey without external guidance.

## Statement

The Identity, CRM and Sales modules are production-ready for a controlled release. All Identity D1-D8, CRM C1-C4, Sales S1-S6 and pilot-readiness P1-P5 findings are resolved or formally documented. The post-hardening audit findings H1, H2, M1, M2, M3 and M4 were closed with regression coverage; security controls are fail-closed, tenant isolation is fail-closed, and the build/test pipeline enforces quality gates on the production provider.
