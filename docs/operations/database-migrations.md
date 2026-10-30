# Database Migration Strategy

The platform persists to PostgreSQL via EF Core (ADR-004). Three modules own schemas:
**Identity**, **CRM** and **Sales**. Migrations are authored per module and applied by the
deployment pipeline.

Each module migration set includes its `outbox_messages` table (ADR-009). The
`AddPlatformOutboxDurability` migration adds retry/claim columns and the unique `EventId` index;
the CRM and Sales migrations create their outbox tables. Apply all pending migrations before
enabling outbox processing.

## Official strategy: migrations are applied by the deployment pipeline

The default, recommended strategy is **out-of-band migration**, applied by tooling during
deployment (not by application instances).

Always restore the repository tooling first so the EF CLI matches the runtime packages
(`dotnet-ef` is pinned in `.config/dotnet-tools.json`):

```bash
dotnet tool restore
```

### Identity

```bash
dotnet ef database update \
  --project src/Modules/Identity/BSX.Modules.Identity.Infrastructure \
  -- --connection "Host=...;Database=bsx_identity;Username=...;Password=..."
```

### CRM

```bash
dotnet ef database update \
  --project src/Modules/Crm/BSX.Modules.Crm.Infrastructure \
  -- --connection "Host=...;Database=bsx_crm;Username=...;Password=..."
```

### Sales

```bash
dotnet ef database update \
  --project src/Modules/Sales/BSX.Modules.Sales.Infrastructure \
  -- --connection "Host=...;Database=bsx_sales;Username=...;Password=..."
```

No `--startup-project` is required: each module's Infrastructure project owns its design-time
factory, so the EF tools use it directly. The factories accept `--connection` (or
`--connection-string`) after `--` and also read `ConnectionStrings__Identity`,
`ConnectionStrings__Crm` and `ConnectionStrings__Sales` from the environment, so build agents can
supply the connection without changing committed files or command lines.

Migration bundles are recommended for production:

```bash
dotnet ef migrations bundle \
  --project src/Modules/Identity/BSX.Modules.Identity.Infrastructure \
  -o ./artifacts/identity-migrate

dotnet ef migrations bundle \
  --project src/Modules/Crm/BSX.Modules.Crm.Infrastructure \
  -o ./artifacts/crm-migrate

dotnet ef migrations bundle \
  --project src/Modules/Sales/BSX.Modules.Sales.Infrastructure \
  -o ./artifacts/sales-migrate
```

Because a migration step can be destructive, it must be a **gated, explicit deployment step** with
a backup, never an implicit side effect of application startup.

## Schemas and migration history

| Module | DbContext | Schema | Migration history | Initial migration |
| --- | --- | --- | --- | --- |
| Identity | `IdentityDbContext` | `identity` | `identity.__ef_migrations_history` | Existing Identity migrations |
| CRM | `CrmDbContext` | `crm` | `crm.__ef_migrations_history` | `InitialCrm` |
| Sales | `SalesDbContext` | `sales` | `sales.__ef_migrations_history` | `InitialSales` |

`ConnectionStrings:Identity`, `ConnectionStrings:Crm` and `ConnectionStrings:Sales` may point to the
**same database** (the default: CRM falls back to Identity; Sales is configured separately once the
host composes the module) or to dedicated databases. The schemas are independent and there are no
cross-schema foreign keys, so the order of the migration commands is not significant. Apply all
**before** rolling out the application version that needs them.

## Optional: apply Identity migrations on startup (single instance only)

For single-instance or non-HA environments, Identity supports:

```json
{ "Identity": { "Database": { "ApplyMigrationsOnStartup": true } } }
```

The host then applies pending Identity migrations during startup
(`IdentityDatabaseMigrationExtensions.ApplyIdentityMigrationsAsync`). Do **not** enable this in a
multi-instance deployment — concurrent migration runs are unsafe. CRM and Sales have no
startup-migration hook; their migrations are always applied out of band.

## Schema incompatibility detection

Enable the schema readiness checks to detect drift at runtime:

```json
{
  "Identity": { "Database": { "VerifySchema": true } },
  "Crm": { "Database": { "VerifySchema": true } },
  "Sales": { "Database": { "VerifySchema": true } }
}
```

This registers `identity-database-schema`, `crm-database-schema` and `sales-database-schema` under
the `ready` tag. Each reports `Unhealthy` when EF detects pending migrations (or when the schema
cannot be inspected), so an orchestrator will not route traffic to an instance running against an
outdated schema. See [health-checks.md](health-checks.md).

## Permission catalog synchronization

Identity seeds/synchronizes system roles from the registered module permission catalogs
(`IPermissionCatalogSource`, ADR-018) after startup migrations. It is controlled by
`Identity:Permissions:SynchronizeOnStartup` (default `true`) and is additive. A missing schema is
logged and skipped. Ensure the Identity migrations have been applied before enabling it in a
production rollout.

## Migration verification in tests

Each module's PostgreSQL parity suite applies the full migration chain to an empty Testcontainers
database and asserts the resulting schema. For Identity,
`Migrations_Should_Apply_AndCreateTheIdentitySchema` verifies the applied chain
(`InitialIdentity` → `AddAuthentication` → `AddPlatformOutboxDurability`), the `identity` tables,
the `Version` concurrency columns, the unique indexes and the cascading foreign keys. See
[postgresql-parity.md](../testing/postgresql-parity.md).

## Guidance

- Always run migrations **before** rolling out the application version that requires them.
- Keep migrations backward compatible for the duration of a rolling deployment (expand/contract).
- Take a database backup before applying destructive migrations.
- Verify `/health/ready` is green (including the schema checks, when enabled) after deployment.
- See [crm-operations.md](crm-operations.md) for CRM-specific deployment and operational guidance
  and [sales/persistence.md](../modules/sales/persistence.md) for Sales schema details.
