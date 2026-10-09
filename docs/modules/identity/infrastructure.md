# Identity - Infrastructure

Implementation of the Identity persistence layer on Entity Framework Core and PostgreSQL
([ADR-004](../../decisions/ADR-004-PostgreSQL.md), [ADR-009](../../decisions/ADR-009-Domain-Event-Dispatching-Strategy.md),
[ADR-010](../../decisions/ADR-010-Auditing-Strategy.md), [ADR-014](../../decisions/ADR-014-Credential-Model-Strategy.md)).

Project: `src/Modules/Identity/BSX.Modules.Identity.Infrastructure`

---

## Provider

- Production: **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL`, schema `identity`.
- Tests: **SQLite in-memory** (relational) for behavior; the model is provider-agnostic. This is a
  test-only substitution because no PostgreSQL instance is available in the build environment.

---

## DbContext and Tables

`IdentityDbContext` maps each aggregate to its own table and applies auditing/soft-delete shadow
properties.

| Aggregate | Table |
| --- | --- |
| `User` | `users` (+ `user_platform_roles`, `password_reset_tokens`, `credentials`) |
| `Role` | `roles` (+ `role_permissions`) |
| `TenantMembership` | `tenant_memberships` (+ `tenant_membership_roles`) |
| `ServiceAccount` | `service_accounts` (+ `service_account_roles`) |
| `ApiKey` | `api_keys` (+ `api_key_scopes`) |
| Outbox | `outbox_messages` |

Credentials use **TPH** on `credential_type` with subtype columns for password, TOTP, recovery
codes, WebAuthn and external providers (ADR-014).

---

## Value Objects and Collections

- Strongly typed identifiers map via `ValueConverter` to `uuid`.
- Single-property value objects (`Email`, `PersonName`, `RoleName`, `SecurityStamp`, token and key
  hashes, `TotpSecret`) map via converters.
- `PasswordHash` maps as an owned type (hash + algorithm columns).
- Value-object collections (`Permission`, `RoleId`, `RecoveryCodeHash`) map as owned collections in
  dedicated tables.

---

## Auditing, Soft Delete and Search

`AuditingSaveChangesInterceptor` (ADR-010):

- stamps `CreatedOnUtc`/`CreatedBy`, `UpdatedOnUtc`/`UpdatedBy`;
- converts hard deletes into soft deletes (`IsDeleted`, `DeletedOnUtc`, `DeletedBy`) and flips the
  state to `Modified`;
- maintains plain `SearchEmail`/`SearchName` shadow columns so the read model can filter without
  touching value-converted columns.

Global query filters exclude soft-deleted rows automatically.

---

## Domain Events, Transactions and Outbox

- `IdentityUnitOfWork` derives from `UnitOfWorkBase` (ADR-009): collect events → persist → commit →
  clear → dispatch.
- `TransactionBehavior` opens the command transaction; a failed `Result` rolls back.
- `OutboxMessage`/`OutboxWriter` persist integration events in the same transaction (currently no
  producer; ready for module integration events).

---

## Repositories

- Write side: `UserRepository`, `RoleRepository`, `TenantMembershipRepository`,
  `ServiceAccountRepository`, `ApiKeyRepository` (aggregate-specific, tracked).
- Read side: `UserReadRepository`, `RoleReadRepository` (DTO projections).

---

## Security Services

- `PasswordHasher` — PBKDF2-SHA256.
- `ApiKeyHasher` — SHA-256.
- `SecureTokenGenerator` — base64url random tokens.
- `SystemCurrentUser` — default `ICurrentUser` (actor `system`) until the host registers the real one.

---

## Registration

```csharp
services.AddIdentityInfrastructure(connectionString);
services.AddIdentityApplication(); // handlers/validators
```

`AddIdentityInfrastructure` registers the DbContext (Npgsql), interceptors, repositories, read
repositories, unit of work, outbox writer and security services.

---

## Migrations

Initial migration: `Persistence/Migrations/*_InitialIdentity.cs`. Generated code is excluded from
analyzers via `.editorconfig`.

```text
dotnet ef migrations add <Name> \
  --project src/Modules/Identity/BSX.Modules.Identity.Infrastructure \
  --startup-project src/Modules/Identity/BSX.Modules.Identity.Infrastructure
```

---

## Notes

- Partial unique indexes for soft-deleted rows (ADR-010) are a follow-up.
- Integration tests run on SQLite; run against PostgreSQL for provider-specific validation.
