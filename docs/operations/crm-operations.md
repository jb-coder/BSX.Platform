# CRM Operations

Operational guidance for the CRM module (customers, contacts, addresses and organizations).

## Deployment requirements

| Requirement | Notes |
| --- | --- |
| PostgreSQL | The module owns the `crm` schema. It may share the Identity database or use a dedicated one. |
| `ConnectionStrings:Crm` | Optional. When omitted, CRM uses `ConnectionStrings:Identity` and creates/uses `crm` in that database. |
| CRM migration | Apply `InitialCrm` (first deployment) out of band before rolling out the host. |
| `Crm:Database:VerifySchema` | Set `true` to add the `crm-database-schema` readiness check. |
| JWT authentication | CRM does not issue tokens; it consumes Identity-issued JWT bearer tokens. |
| Permissions | `crm.customers.{read,create,update}` and `crm.organizations.{read,create,update}`. |

See [deployment-checklist.md](deployment-checklist.md) and
[database-migrations.md](database-migrations.md).

## Schema

Schema: `crm`. Migration history: `crm.__ef_migrations_history`.

| Table | Contents |
| --- | --- |
| `customers` | Customer aggregate: code, name, tax identifier, organization reference, status, audit/soft-delete/concurrency columns. |
| `customer_addresses` | Owned addresses (postal columns + type + default flag). |
| `customer_contacts` | Owned contact persons (name + optional email/phone + primary flag). |
| `organizations` | Organization aggregate: name, tax identifier, optional postal address. |

Constraints and indexes of note:

- `CK_customers_tenant_key` / `CK_organizations_tenant_key` enforce `TenantKey = TenantId`.
- `IX_customers_TenantId_Code` is a **filtered unique index** (`"IsDeleted" = false`): customer
  codes are unique per tenant and reusable after a soft delete.
- Tenant/search indexes support tenant-scoped reads.

## Migrations

```bash
dotnet tool restore
dotnet ef database update \
  --project src/Modules/Crm/BSX.Modules.Crm.Infrastructure \
  -- --connection "Host=...;Database=...;Username=...;Password=..."
```

The design-time factory also reads `ConnectionStrings__Crm`. CRM migrations are always applied out
of band; the host never applies them on startup.

## Health

- `crm-database` (`ready`) — the CRM database is reachable.
- `crm-database-schema` (`ready`, opt-in) — no pending CRM migrations.

When CRM shares the Identity database, both `identity-database` and `crm-database` are evaluated
against the same server/database from their respective contexts.

## Tenant isolation

CRM is multi-tenant and **fail-closed**:

1. Every repository call takes an explicit tenant identifier; reads and writes are tenant-scoped.
2. `CrmDbContext` applies a global filter:
   `!IsDeleted && (IsSystem || (CurrentTenantId != null && TenantKey == CurrentTenantId))`.
   A request without a tenant sees **no CRM rows**.
3. `TenantKey` is populated from the aggregate's `TenantId` by the auditing interceptor; the
   database check constraint prevents divergence.
4. Cross-tenant detail reads return `404`; cross-tenant lists return empty pages.

The tenant is always resolved from the authenticated token, never from client input.

### Tenant scope

`CrmTenantScope` resolves the tenant from `ITenantContext`. Background/operations code may widen
the scoped instance explicitly:

- `UseTenant(tenantId)` — operate on one tenant (per-tenant background job).
- `UseSystemScope()` — privileged cross-tenant operation; the soft-delete filter still applies.

Request paths must never call these methods. Treat `UseSystemScope` as a privileged operation and
restrict it to trusted background code.

## Concurrency

Each aggregate has a shadow `Version` concurrency token. The audit interceptor increments it on
update; EF includes the original value in the `UPDATE` predicate. A stale write affects zero rows
and the unit of work raises `ConcurrencyConflictException`.

The CRM endpoint filter maps that exception to:

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "title": "CRM.ConcurrencyConflict",
  "detail": "The resource was modified by another request. Reload it and retry.",
  "status": 409
}
```

The rejected write is not persisted. Clients should re-read the resource and retry. CRM does not
currently emit `ETag`/`If-Match`; the version is server-side only.

## Search

- Customers: case-insensitive partial match on **code** or **name**.
- Organizations: case-insensitive partial match on **name** or **tax identifier**.

Search uses lower-cased denormalized columns (`LIKE`), so it behaves the same on PostgreSQL and
the SQLite test provider. A leading-wildcard search cannot use the B-tree indexes; for large
datasets introduce PostgreSQL `pg_trgm` GIN indexes or prefix search.

Empty/whitespace search returns all rows (subject to tenant and soft-delete filters). Paging is
normalized on the server (page ≥ 1; size 1–100, default 20).

## Soft delete

Deleting a customer or organization is a soft delete: `IsDeleted = true` with `DeletedOnUtc` /
`DeletedBy`. Rows are retained for audit and excluded from all reads. Customer codes can be reused
after a soft delete because the unique index is filtered on `IsDeleted = false`.

## Permissions

| Operation | Permission |
| --- | --- |
| Read customers | `crm.customers.read` |
| Create/update customers, contacts, addresses, organization assignment | `crm.customers.create` / `crm.customers.update` |
| Read organizations | `crm.organizations.read` |
| Create/update organizations | `crm.organizations.create` / `crm.organizations.update` |

Missing permissions return `403`; unauthenticated calls return `401`.

## Troubleshooting

| Symptom | Likely cause | Action |
| --- | --- | --- |
| Empty CRM lists | No tenant in the token (fail-closed) or wrong tenant | Verify the login selected the intended tenant; inspect the token claims. |
| `404` on a known CRM resource | Resource belongs to another tenant | Confirm the tenant; cross-tenant access is intentionally hidden. |
| `409 CRM.ConcurrencyConflict` | Concurrent update of the same aggregate | Re-read and retry. |
| `503` readiness with `crm-database` | Database unreachable | Check network/credentials; when shared, check the Identity database too. |
| `503` readiness with `crm-database-schema` | Pending CRM migrations | Apply the CRM migration bundle. |
| `409 CRM.Customer.CodeAlreadyExists` | Duplicate code in the tenant | Choose another code. Soft-deleted codes are reusable. |

## Testing

- Unit/application/domain tests run without infrastructure.
- Infrastructure and endpoint integration tests run against SQLite in memory.
- `PostgreSqlParityTests` (Testcontainers) validates the production provider: migrations, schema,
  filtered unique index, tenant filters, tenant-key check constraints, search, soft delete and
  optimistic concurrency. They are skipped when Docker is unavailable; set
  `DOCKER_AVAILABLE=true` to require Docker and fail when it is missing.

## Known limitations

- No UI, reporting or analytics.
- No per-tenant rate limiting/quotas (platform-wide gap; use a gateway).
- No `ETag`/`If-Match` precondition support.
- Cross-module commands are not atomic (the composite unit of work commits per module); keep
  commands module-scoped.
