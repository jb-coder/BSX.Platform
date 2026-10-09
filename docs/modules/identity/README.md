# Identity Module

> Authentication, authorization, RBAC and permissions for BSX Platform.

| | |
| --- | --- |
| Status | Planned (Phase 2) |
| Bounded context | Identity and Access Management |
| Owner | Platform |
| Planned projects | `src/Modules/Identity/{Domain,Application,Infrastructure,Contracts,Endpoints}` |
| Public surface | `Identity.Contracts` only |

This folder is the design specification for the Identity module. It contains no production code.

---

## Overview

Identity is the platform module that answers two questions:

- **Who is the caller?** (authentication)
- **What may the caller do?** (authorization)

It owns users, roles, permissions and refresh-token sessions. It issues access and refresh tokens
and it implements the platform authorization abstractions (`ICurrentUser`, `IPermissionChecker`)
defined in [ADR-012](../../decisions/ADR-012-Authorization-Architecture.md).

Identity is a business capability of the modular monolith and follows every architecture rule:
module boundaries, CQRS, Vertical Slice, DDD, Result pattern and the Outbox (see
[architecture rules](#architecture-rules)).

---

## Scope

### In scope

- User lifecycle (registration, profile, activation, locking).
- Credential management (password hashing, change, reset).
- Role and permission management (RBAC).
- Authentication and token issuance.
- Refresh-token sessions, rotation and revocation.

### Out of scope

- Business profiles (Customer, Contact) — owned by CRM.
- Tenant lifecycle and provisioning — future multi-tenancy phase.
- Notifications delivery (email) — Identity publishes integration events; Notifications sends.
- External identity providers and SSO (a future ADR if required).

---

## Business Capabilities

| Capability | Description |
| --- | --- |
| User management | Register, update, activate, deactivate, lock and unlock users. |
| Credential management | Set, change and reset passwords. |
| Role management | Create, rename, delete roles and assign permissions to roles. |
| Permission catalog | Declare, seed and expose the platform permission catalog. |
| Role assignment | Assign roles to users and manage effective permissions. |
| Authentication | Verify credentials and start a session. |
| Token issuance | Issue access tokens and refresh tokens. |
| Session management | List, refresh and revoke sessions. |
| Authorization provider | Implement `ICurrentUser` and `IPermissionChecker` for the platform. |

---

## Architecture Rules

Identity is bound by all platform ADRs. The most relevant:

- [ADR-002 CQRS](../../decisions/ADR-002-CQRS.md)
- [ADR-003 DDD](../../decisions/ADR-003-DDD.md)
- [ADR-004 PostgreSQL](../../decisions/ADR-004-PostgreSQL.md)
- [ADR-009 Domain Event Dispatch](../../decisions/ADR-009-Domain-Event-Dispatching-Strategy.md)
- [ADR-010 Auditing](../../decisions/ADR-010-Auditing-Strategy.md)
- [ADR-011 Result and Error Mapping](../../decisions/ADR-011-Result-And-Error-Mapping-Strategy.md)
- [ADR-012 Authorization](../../decisions/ADR-012-Authorization-Architecture.md)
- [ADR-013 Multi-Tenant Identity](../../decisions/ADR-013-Multi-Tenant-Identity-Strategy.md)
- [ADR-014 Credential Model](../../decisions/ADR-014-Credential-Model-Strategy.md)
- [ADR-015 Contracts Abstraction](../../decisions/ADR-015-Contracts-Abstraction-Strategy.md)
- [ADR-016 Password Reset Security](../../decisions/ADR-016-Password-Reset-Security-Strategy.md)
- [ADR-017 Token Invalidation and Sessions](../../decisions/ADR-017-Token-Invalidation-And-Session-Strategy.md)
- [ADR-018 Role Lifecycle and Permission Cache](../../decisions/ADR-018-Role-Lifecycle-And-Permission-Cache-Strategy.md)

> **Design amendments:** the behavior in this folder is refined by
> [design-amendments.md](design-amendments.md) (ADR-013–018). Where they disagree, the amendment
> wins.

Conventions: [implementation-guidelines.md](../../architecture/implementation-guidelines.md),
[docs/diagrams/module-structure.md](../../diagrams/module-structure.md),
[docs/diagrams/cqrs-pipeline.md](../../diagrams/cqrs-pipeline.md).

---

## Documents

| Document | Contents |
| --- | --- |
| [design-amendments.md](design-amendments.md) | Authoritative changes from ADR-013–018. |
| [domain-model.md](domain-model.md) | Aggregates, entities, value objects, domain events, invariants. |
| [application.md](application.md) | Commands and queries. |
| [permissions-model.md](permissions-model.md) | Permissions, roles, catalog and enforcement. |
| [jwt-strategy.md](jwt-strategy.md) | Access token design, claims, keys and validation. |
| [refresh-token-strategy.md](refresh-token-strategy.md) | Sessions, rotation, reuse detection and revocation. |
| [api-surface.md](api-surface.md) | Endpoints, permissions, requests and responses. |
| [module-boundaries.md](module-boundaries.md) | Contracts, integration events and data ownership. |
| [diagrams.md](diagrams.md) | Context, aggregate, ER, login and refresh diagrams. |
