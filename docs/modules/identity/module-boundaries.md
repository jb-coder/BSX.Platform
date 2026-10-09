# Identity - Module Boundaries

Identity is a business module of the modular monolith. This document defines its public surface,
its dependencies and the rules other modules must respect
([module-boundaries.md](../../architecture/module-boundaries.md),
[module-structure.md](../../diagrams/module-structure.md)).

---

## Projects

```text
src/Modules/Identity
├── Identity.Domain          Aggregates, entities, value objects, domain events
├── Identity.Application     Commands, queries, handlers, validators, ports
├── Identity.Infrastructure  EF Core, identity schema, repositories, outbox, provisioning
├── Identity.Contracts       Integration events and integration DTOs (public surface)
└── Identity.Endpoints       Minimal API endpoints, IErrorMapper usage
```

Dependency direction (enforced by architecture tests):

- `Domain` → `BSX.SharedKernel` only.
- `Application` → `Domain`, `BSX.BuildingBlocks`, `BSX.SharedKernel`.
- `Infrastructure` → `Application`, `Domain`.
- `Endpoints` → `Application`.
- `Contracts` → `BSX.Contracts` only (ADR-015). It carries integration events, integration DTOs
  and shared identifiers (`UserId`, `RoleId`, `TenantId`); it never references a module's Domain,
  Application or Infrastructure.

No other module may reference Identity's Domain, Application, Infrastructure or Endpoints.

---

## Public Surface

`Identity.Contracts` is the **only** project other modules may reference. It contains:

- Integration events.
- Integration DTOs and strongly typed identifiers safe to share (`UserId`, `RoleId`).
- Permission code constants for modules that declare permissions.

It contains **no** domain types, no entities, no persistence types.

---

## Integration Events

Published through the Outbox and consumed by other modules (ADR-009). Integration events are
versioned, additive contracts.

| Integration event | Published when | Likely consumers |
| --- | --- | --- |
| `UserRegisteredIntegrationEvent` | A user is registered. | Notifications (welcome), CRM. |
| `UserDeactivatedIntegrationEvent` | A user is deactivated. | Notifications, Auditing. |
| `UserEmailChangedIntegrationEvent` | A user email changes. | Notifications, CRM. |
| `UserRolesChangedIntegrationEvent` | Roles/permissions change. | Any module caching authorization. |
| `UserPasswordResetRequestedIntegrationEvent` | A reset is requested. | Notifications (delivery). |

Naming of integration events never collides with domain events
([domain-model.md](domain-model.md)).

---

## Permission Declaration

Modules must not make Identity depend on them. Instead:

- Each module declares its permissions through an `IPermissionCatalogContributor` abstraction.
- The host collects all contributors at composition time.
- Identity's `SynchronizePermissions` seeds the catalog from the collected set.

This keeps Identity decoupled from other modules while guaranteeing a single permission catalog.

---

## Dependencies

### Identity depends on

- `BSX.SharedKernel`, `BSX.BuildingBlocks`.
- PostgreSQL and EF Core (Infrastructure only).
- The host for composition (`IModule`, authentication middleware).

### Identity is consumed by

- The **host** (`BSX.Web`): configures the authentication scheme (Bearer for APIs, cookie for the
  Blazor UI), registers Identity through `IModule`, and consumes the JWKS endpoint.
- **Other modules** for authorization: through the platform abstractions `ICurrentUser` and
  `IPermissionChecker` ([ADR-012](../../decisions/ADR-012-Authorization-Architecture.md)), never
  through Identity internals.
- **Notifications** for delivery: through integration events.
- **CRM**: may store `UserId` from `Identity.Contracts` to associate a platform user with a
  customer contact, referenced by id only.

---

## Data Ownership

- Identity owns the `identity` PostgreSQL schema.
- No other module reads or writes Identity tables; consistency is maintained through integration
  events.
- Cross-schema foreign keys between modules are forbidden; modules reference each other by id.

---

## Forbidden

- Referencing `Identity.Domain`, `Identity.Application`, `Identity.Infrastructure` or
  `Identity.Endpoints` from another module.
- Reading or writing `identity.*` tables directly.
- Sharing domain types or EF entities across modules.
- Publishing domain events across module boundaries (ADR-009).

---

## Multi-Tenancy Readiness

- The reserved `TenantId` shadow property and the reserved `tenant` token claim are present from
  the start (ADR-010).
- Users, roles and sessions are designed to be tenant-scoped without a schema change when the
  tenancy phase begins.
