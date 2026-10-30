# PostgreSQL Parity Testing

This document describes the provider-parity strategy for the BSX platform, with specific focus on
the **Identity** module. The production database is PostgreSQL (ADR-004); most integration tests run
on SQLite in memory for speed. The parity suites close the gap by exercising the same persistence,
constraint and security behaviors against the production provider.

The approach is identical for Identity, CRM and Sales: a shared Testcontainers PostgreSQL
environment, migrated with the module's real migration chain, and focused tests for the behaviors
that SQLite cannot prove.

---

## Strategy

### Real PostgreSQL via Testcontainers

Each module's infrastructure integration test project contains:

- `TestInfrastructure/DockerAvailableFactAttribute.cs` — skips parity tests when Docker is absent,
  unless `DOCKER_AVAILABLE=true` is set, in which case a missing daemon fails loudly.
- `TestInfrastructure/PostgreSql<Module>Environment.cs` — starts one `postgres:17-alpine`
  container per test process, applies the full module migration chain with
  `Database.MigrateAsync()`, and exposes a production-shaped service provider.

The container and migrated schema are shared by every parity test in the process. Tests isolate
themselves with unique identifiers (emails, tenant ids, role names), so they can run in parallel
against one database without cleanup steps.

### Production registration, not a test-only model

The Identity environment calls the real registrations:

```csharp
services.AddIdentityApplication();
services.AddIdentityInfrastructure(connectionString);
services.AddCompositeUnitOfWork();
```

This means the parity tests exercise the production `IdentityDbContext`, Npgsql provider,
migration history table (`identity.__ef_migrations_history`), auditing interceptor, outbox
interceptor, repositories, `PermissionResolver`, `AuthorizationSnapshotReader` and
`AccountSecurityStore` — not a hand-built approximation.

### Skip cleanly, fail loudly on request

- Without Docker, parity tests are reported as **skipped** with an explanatory message; the default
  local suite stays green.
- With `DOCKER_AVAILABLE=true`, a missing Docker daemon is a failure, so CI can require parity.
- GitHub-hosted Ubuntu runners provide Docker; the CI pipeline runs the parity suites there.

---

## Running the suites

Identity parity only:

```bash
dotnet test tests/Modules/Identity/BSX.Modules.Identity.Infrastructure.IntegrationTests \
  --filter "FullyQualifiedName~PostgreSql"
```

Require Docker (never skip):

```bash
DOCKER_AVAILABLE=true dotnet test BSX.Platform.sln -c Release
```

CRM and Sales parity use the same pattern:

```bash
dotnet test tests/Modules/Crm/BSX.Modules.Crm.Infrastructure.IntegrationTests \
  --filter "FullyQualifiedName~PostgreSql"
dotnet test tests/Modules/Sales/BSX.Modules.Sales.Infrastructure.IntegrationTests \
  --filter "FullyQualifiedName~PostgreSql"
```

---

## Identity coverage

The Identity parity suite lives in
`tests/Modules/Identity/BSX.Modules.Identity.Infrastructure.IntegrationTests`:

- `PostgreSqlMigrationTests.cs` — clean-database migration chain, schema, indexes, concurrency
  columns and foreign keys.
- `PostgreSqlParityTests.cs` — persistence, constraints, concurrency, transactions and aggregate
  round-trips.
- `PostgreSqlSecurityParityTests.cs` — lockout hardening, permission scope and authorization
  revalidation dependencies.

### Persistence and schema

| Scenario | Test |
| --- | --- |
| Full migration chain applies; no pending migrations; expected tables, `Version` columns, unique indexes and cascading foreign keys exist | `Migrations_Should_Apply_AndCreateTheIdentitySchema` |
| User create/read, credential persistence, globally unique email | `User_Should_RoundTrip_AndEnforceUniqueEmail` |
| User update, activation, deactivation and security-stamp rotation | `User_Should_SupportUpdateActivateAndDeactivate` |
| Role create/edit, permission grant/revoke, unique name per tenant, unique system-role name | `Role_Should_RoundTripPermissions_AndEnforceNamePerTenant` |
| Membership create, role change, removal, multi-tenant handling and uniqueness | `Membership_Should_RoundTripRoles_AndSupportMultiTenantHandling` |
| Session creation, refresh rotation, revocation and stamp persistence | `Session_Should_RoundTripRotationRevocation_AndSecurityStamp` |
| Refresh-token reuse outside the grace window revokes the session | `Session_Should_DetectRefreshTokenReuse_OutsideTheGraceWindow` |
| Logout-all revokes every active session | `LogoutAll_Should_RevokeEveryActiveSession` |
| Optimistic concurrency on user and session rows | `Concurrency_Should_DetectStaleUserAndSessionUpdates` |
| Transaction rollback; atomic user + outbox persistence | `Transactions_Should_RollBack_AndPersistTheOutboxAtomically` |
| Soft delete retains rows and hides them from queries | `SoftDelete_Should_RetainRows_AndExcludeThemFromQueries` |

### Security hardening

| Scenario | Test | ADR |
| --- | --- | --- |
| Threshold lockout persists, rotates the stamp and revokes sessions on PostgreSQL | `Lockout_Should_PersistAndRevokeSessions_OnPostgreSql` | ADR-022 |
| Concurrent failed logins do not lose increments (PostgreSQL advisory lock) | `ConcurrentFailures_Should_BeSerialized_ByTheAdvisoryLock` | ADR-022 |
| A concurrent session revocation cannot roll back the lockout or the stamp | `ConcurrentSessionRevoke_ShouldNotRollBackLockout_OnPostgreSql` | ADR-022 |
| Platform vs tenant permission scopes are enforced; a tenant role cannot grant a platform permission | `PermissionResolution_Should_EnforceScopeBoundaries_OnPostgreSql` | ADR-020 |
| Revalidation depends on session status, user status and security stamp | `AuthorizationSnapshot_Should_DependOnStatusSessionAndStamp_OnPostgreSql` | ADR-021 |
| Refresh rejection revokes the session durably | `RevokeSession_Should_PersistDurably_OnPostgreSql` | ADR-022 |

### Migration verification

`PostgreSqlMigrationTests.Migrations_Should_Apply_AndCreateTheIdentitySchema` validates the
production schema directly:

- `GetPendingMigrationsAsync()` is empty and the applied chain contains
  `InitialIdentity` → `AddAuthentication` → `AddPlatformOutboxDurability`.
- The `identity` schema contains the user, role, membership, credential, session and outbox tables.
- The `Version` optimistic-concurrency columns exist on `users`, `roles`, `tenant_memberships`,
  `service_accounts`, `api_keys` and `user_sessions`.
- The unique indexes `IX_users_Email`, `IX_roles_TenantId_Name`, `IX_roles_Name`,
  `IX_tenant_memberships_TenantId_UserId` and `IX_outbox_messages_EventId` are generated as unique.
- The credential, refresh-token and membership-role foreign keys are generated with `ON DELETE
  CASCADE`.

The environment is created from an empty database and migrated with the full chain, so this also
covers the clean-database upgrade path.

---

## SQLite vs PostgreSQL differences covered

| Concern | SQLite tests | PostgreSQL parity |
| --- | --- | --- |
| Schema | None (SQLite ignores schemas) | `identity` schema + `identity.__ef_migrations_history` |
| Advisory lock | Not available; writes serialize | `pg_advisory_xact_lock` per user for failure increments |
| Unique indexes | Enforced, but index metadata differs | Filtered unique indexes and constraint names verified |
| Foreign keys | Enforced when enabled | `ON DELETE CASCADE` verified through `information_schema` |
| `Guid`/`DateTimeOffset` | Text storage | Native `uuid`/`timestamptz` |
| Concurrency tokens | Enforced | Enforced; stale updates surface as `ConcurrencyConflictException` |
| Transactions/outbox | Enforced | Enforced atomically with the aggregate state |

---

## Findings

No behavioral divergence between SQLite and PostgreSQL was found during this work. All Identity
parity tests pass against PostgreSQL. The differences that matter are provider capabilities, not
observable behavior:

- The advisory lock is PostgreSQL-specific; a dedicated test proves concurrent failure increments
  are not lost.
- Filtered unique indexes, schema-qualified tables and `ON DELETE CASCADE` are generated by the
  migrations and verified directly against `information_schema`/`pg_indexes`.
- The ADR-022 lockout/session-revocation separation is exercised with a real PostgreSQL row lock,
  proving the lockout commits independently of session cleanup.

## Results

- Identity infrastructure integration suite: **60 tests** (43 existing SQLite + 17 PostgreSQL
  parity), **0 failed, 0 skipped** with Docker available.
- Full solution suite: **1116 tests**, **0 failed, 0 skipped**.
- Coverage added: 17 provider-level tests covering migrations, schema constraints, users, roles,
  memberships, sessions, refresh rotation/reuse/revocation, concurrency, transactions/outbox, soft
  delete, lockout hardening, permission scope and authorization revalidation.

---

## Remaining limitations

- **Application handlers are not re-run against PostgreSQL.** Login, refresh, logout and the HTTP
  pipeline are covered by the web integration suite on SQLite. The Identity parity suite validates
  the persistence and security primitives those handlers depend on (sessions, refresh chains,
  lockout store, permission resolver, revalidation reader). Re-hosting the full ASP.NET pipeline
  against PostgreSQL would duplicate the web suite without adding provider-specific evidence.
- **Lockout expiry and time-window behavior** are exercised on SQLite with a controllable clock.
  The PostgreSQL suite uses real time and focuses on persistence, the advisory lock and concurrency.
- **Rate limiting remains process-local** (documented in ADR-022 and the rate-limiting guide); the
  parity suite does not change that.
- **The outbox background processor is not started** in the parity environment; the tests verify
  atomic outbox writes, not the recovery loop.
- **Docker is required.** Without it the parity tests skip. CI provides Docker; locally, set
  `DOCKER_AVAILABLE=true` to make the requirement explicit.

---

## Maintenance rules

- Add a PostgreSQL parity test when a new aggregate, unique index, foreign key, check constraint or
  concurrency token is introduced.
- Add a parity test when a security control depends on provider behavior (locks, transactions,
  filtered indexes).
- Keep parity tests focused on behavior SQLite cannot prove; do not duplicate the SQLite suite.
- Keep the shared environment migrated with the real migration chain; never use `EnsureCreated` for
  parity tests.
