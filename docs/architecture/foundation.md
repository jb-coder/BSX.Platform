# Foundation

## Purpose

This document describes the technical foundation of BSX Platform: the solution layout, the
platform projects, the build configuration, the dependency rules and the hardening decisions that
every module must respect.

The foundation is deliberately limited to the platform kernel. Business modules are not
implemented yet.

---

## Solution Structure

```text
BSX.Platform
│
├── src
│   ├── BSX.SharedKernel        Platform kernel (DDD primitives + Result)
│   ├── BSX.Contracts           Cross-module contract primitives (integration events, shared ids)
│   ├── BSX.BuildingBlocks      Technical cross-cutting abstractions
│   ├── BSX.Web                 Blazor Web App composition root
│   ├── Modules                 Business modules (planned, no projects yet)
│   └── Platform                Platform services (planned, no projects yet)
│
├── tests
│   ├── BSX.SharedKernel.UnitTests
│   ├── BSX.BuildingBlocks.UnitTests
│   └── BSX.ArchitectureTests
│
├── docs
├── infrastructure
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
└── BSX.Platform.sln
```

`Modules` and `Platform` exist as solution folders only.

---

## Projects

### BSX.SharedKernel

The domain kernel. Zero external dependencies, database agnostic, transport agnostic.

Contents: `Entity<TId>`, `AggregateRoot<TId>`, `IHasDomainEvents`, `ValueObject`,
`IDomainEvent`/`DomainEvent`, `Result`/`Result<TValue>`, `Error`/`ErrorKind`.

### BSX.BuildingBlocks

Reusable technical patterns shared by every module. References `BSX.SharedKernel` and
`BSX.Contracts`.

Contents: CQRS abstractions and dispatcher, the canonical pipeline behaviors, domain/integration
event abstractions, persistence abstractions, authorization abstractions and the `IModule`
composition contract.

### BSX.Web

The composition root. A Blazor Web App (.NET 10) that references the kernel and the building
blocks, configures Serilog and OpenTelemetry, registers the platform services, composes modules
through `IModule` and hosts the application. It contains no business features.

---

## Dependency Rules

```text
BSX.Web  ──►  BSX.BuildingBlocks  ──►  BSX.SharedKernel
                     │
                     └──►  BSX.Contracts  ──►  BSX.SharedKernel

<Module>.Contracts  ──►  BSX.Contracts
```

- `BSX.SharedKernel` depends on nothing.
- `BSX.Contracts` owns integration-event primitives and shared identifiers (ADR-015).
- `BSX.BuildingBlocks` may depend on `BSX.SharedKernel` and `BSX.Contracts`.
- `BSX.Web` may depend on the platform projects.
- Modules may depend on the kernel, contracts and building blocks; the internal module direction is
  defined in [module-structure.md](../diagrams/module-structure.md).
- Modules must never depend on each other's implementation; they communicate through integration
  events and `<Module>.Contracts`.

These rules are enforced by the architecture tests in `tests/BSX.ArchitectureTests`.

---

## Request Pipeline

Canonical order (outermost first): **Logging → Validation → Authorization → Transaction
(commands) → Handler**. See [cqrs-pipeline.md](../diagrams/cqrs-pipeline.md).

---

## Build Configuration

| File | Responsibility |
| --- | --- |
| `global.json` | Pins the .NET 10 SDK feature band. |
| `Directory.Build.props` | Shared compiler settings: nullable, implicit usings, warnings as errors, deterministic builds, documentation generation. |
| `Directory.Packages.props` | Central Package Management: a single source of truth for NuGet versions. |
| `.editorconfig` | Code style and naming rules, enforced during build. |

Rules:

- Package versions are declared once, in `Directory.Packages.props`.
- Every project targets `net10.0`.
- Adding a package requires a version entry in the central file.

---

## Testing

| Suite | Responsibility |
| --- | --- |
| `BSX.SharedKernel.UnitTests` | DDD primitives and the Result pattern. |
| `BSX.BuildingBlocks.UnitTests` | Dispatcher, pipeline behaviors and domain event dispatch. |
| `BSX.ArchitectureTests` | Enforces dependency direction, naming and sealing rules. |

---

## Architecture Hardening

The foundation review produced Critical/High findings. They are closed by the following
decisions:

| Finding | Resolution |
| --- | --- |
| Domain events collected but never dispatched | [ADR-009](../decisions/ADR-009-Domain-Event-Dispatching-Strategy.md) — post-commit dispatch via a transactional Outbox. |
| Auditing interfaces forced Domain → BuildingBlocks | [ADR-010](../decisions/ADR-010-Auditing-Strategy.md) — auditing moved to shadow properties; interfaces removed. |
| Transport semantics in the kernel; flat validation errors | [ADR-011](../decisions/ADR-011-Result-And-Error-Mapping-Strategy.md) — semantic `ErrorKind`, structured errors, adapter-only mapping. |
| `ICurrentUser` unregistered; no authorization behavior | [ADR-012](../decisions/ADR-012-Authorization-Architecture.md) — always-registered defaults, fail-closed checker, authorization behavior. |
| No transaction enforcement or dispatch seam for commands | `TransactionBehavior` + `IUnitOfWork.SaveChangesAndDispatchAsync` + `UnitOfWorkBase` (ADR-009) — see [persistence-dispatch.md](persistence-dispatch.md). |
| No module composition contract | `IModule` — see [module-structure.md](../diagrams/module-structure.md). |
| Contracts/`IIntegrationEvent` dependency contradiction | [ADR-015](../decisions/ADR-015-Contracts-Abstraction-Strategy.md) — `BSX.Contracts` owns integration-event primitives. |
| Single-tenant identity model blocked multi-company/SaaS | [ADR-013](../decisions/ADR-013-Multi-Tenant-Identity-Strategy.md) — global identity + tenant membership. |
| Single `PasswordCredential` blocked MFA/passkeys/API keys | [ADR-014](../decisions/ADR-014-Credential-Model-Strategy.md) — principal + hybrid credential model. |
| Reset token persisted in Outbox | [ADR-016](../decisions/ADR-016-Password-Reset-Security-Strategy.md) — hashed token + encrypted sensitive payloads. |
| Security stamp / token invalidation / session gaps | [ADR-017](../decisions/ADR-017-Token-Invalidation-And-Session-Strategy.md). |
| Role deletion, system-role sync, permission cache gaps | [ADR-018](../decisions/ADR-018-Role-Lifecycle-And-Permission-Cache-Strategy.md). |

The concrete conventions every module must follow are in
[implementation-guidelines.md](implementation-guidelines.md).

---

## Out of Scope

The foundation does not implement:

- Identity
- CRM
- Any business module
- Persistence implementation (only abstractions and the required kernel changes are defined)
- The final UI design system

---

## Extending the Platform

See [implementation-guidelines.md](implementation-guidelines.md). It defines the module
structure, the composition contract, and the conventions for CQRS, validation, results,
authorization, auditing, persistence, observability and testing.
