# Architecture Decision Records

Architecture Decision Records for BSX Platform. An ADR is created for every decision that
affects long-term maintainability, module boundaries, or the technology baseline.

| ADR | Title | Status |
| --- | --- | --- |
| [ADR-001](ADR-001-Modular-Monolith.md) | Modular Monolith Architecture | Accepted |
| [ADR-002](ADR-002-CQRS.md) | CQRS | Accepted |
| [ADR-003](ADR-003-DDD.md) | Domain Driven Design | Accepted |
| [ADR-004](ADR-004-PostgreSQL.md) | PostgreSQL | Accepted |
| [ADR-005](ADR-005-GitFlow.md) | GitFlow | Accepted |
| [ADR-006](ADR-006-Shared-Kernel-And-Building-Blocks.md) | Shared Kernel and Building Blocks Separation | Accepted |
| [ADR-007](ADR-007-In-Process-CQRS-Dispatcher.md) | In-Process CQRS Dispatcher | Accepted |
| [ADR-008](ADR-008-Central-Package-Management.md) | Central Package Management and Build Governance | Accepted |
| [ADR-009](ADR-009-Domain-Event-Dispatching-Strategy.md) | Domain Event Dispatching Strategy | Accepted |
| [ADR-010](ADR-010-Auditing-Strategy.md) | Auditing Strategy | Accepted |
| [ADR-011](ADR-011-Result-And-Error-Mapping-Strategy.md) | Result and Error Mapping Strategy | Accepted |
| [ADR-012](ADR-012-Authorization-Architecture.md) | Authorization Architecture | Accepted |
| [ADR-013](ADR-013-Multi-Tenant-Identity-Strategy.md) | Multi-Tenant Identity Strategy | Accepted |
| [ADR-014](ADR-014-Credential-Model-Strategy.md) | Credential Model Strategy | Accepted |
| [ADR-015](ADR-015-Contracts-Abstraction-Strategy.md) | Contracts Abstraction Strategy | Accepted |
| [ADR-016](ADR-016-Password-Reset-Security-Strategy.md) | Password Reset Security Strategy | Accepted |
| [ADR-017](ADR-017-Token-Invalidation-And-Session-Strategy.md) | Token Invalidation, Security Stamp and Session Strategy | Accepted |
| [ADR-018](ADR-018-Role-Lifecycle-And-Permission-Cache-Strategy.md) | Role Lifecycle and Permission Cache Strategy | Accepted |

## Conventions

- One decision per ADR, using [ADR-Template.md](ADR-Template.md).
- Status is one of Proposed, Accepted, Deprecated, Superseded.
- Once Accepted, an ADR is not edited for substance; a superseding ADR is created instead.
- Every architectural change updates the affected ADR and the [architecture docs](../architecture/foundation.md).
