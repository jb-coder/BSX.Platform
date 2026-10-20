# Database Migration Strategy (D2)

Identity persists to PostgreSQL via EF Core (ADR-004). Migrations are authored in
`BSX.Modules.Identity.Infrastructure/Persistence/Migrations`.

## Official strategy: migrations are applied by the deployment pipeline

The default, recommended strategy is **out-of-band migration**, applied by tooling during
deployment (not by application instances):

```bash
# From a build agent with the connection string available:
dotnet ef database update \
  --project src/Modules/Identity/BSX.Modules.Identity.Infrastructure \
  --startup-project src/BSX.Web \
  -- --connection "Host=...;Database=bsx_identity;Username=...;Password=..."
```

Migration bundles are recommended for production:

```bash
dotnet ef migrations bundle \
  --project src/Modules/Identity/BSX.Modules.Identity.Infrastructure \
  --startup-project src/BSX.Web -o ./artifacts/identity-migrate
```

Because a migration step can be destructive, it must be a **gated, explicit deployment step** with
a backup, never an implicit side effect of application startup.

## Optional: apply on startup (single instance only)

For single-instance or non-HA environments, set:

```json
{ "Identity": { "Database": { "ApplyMigrationsOnStartup": true } } }
```

The host then applies pending migrations during startup
(`IdentityDatabaseMigrationExtensions.ApplyIdentityMigrationsAsync`). Do **not** enable this in a
multi-instance deployment — concurrent migration runs are unsafe.

## Schema incompatibility detection

Enable the schema readiness check to detect drift at runtime:

```json
{ "Identity": { "Database": { "VerifySchema": true } } }
```

This registers `identity-database-schema` under the `ready` tag. It reports `Unhealthy` when EF
detects pending migrations (or when the schema cannot be inspected), so an orchestrator will not
route traffic to an instance running against an outdated schema. See
[health-checks.md](health-checks.md).

## Guidance

- Always run migrations **before** rolling out the application version that requires them.
- Keep migrations backward compatible for the duration of a rolling deployment (expand/contract).
- Take a database backup before applying destructive migrations.
- Verify `/health/ready` is green (including `identity-database-schema`) after deployment.
