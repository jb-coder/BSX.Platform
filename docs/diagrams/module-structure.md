# Module Structure

Every module follows the same internal structure (Clean Architecture per module) with strict
dependency direction.

## Layout

```text
src/Modules/<Module>
├── <Module>.Domain          Aggregates, entities, value objects, domain events, rules
├── <Module>.Application     Commands, queries, handlers, validators, DTOs, ports
├── <Module>.Infrastructure  EF Core DbContext, configurations, repositories, outbox
├── <Module>.Contracts       Published contracts and integration events (only public surface)
└── <Module>.Endpoints       Minimal API endpoints / transport mapping

tests/Modules/<Module>
├── <Module>.UnitTests
└── <Module>.IntegrationTests
```

## Platform Projects Referenced by Modules

| Project | Purpose |
| --- | --- |
| `BSX.SharedKernel` | Domain primitives and `Result`. |
| `BSX.Contracts` | Cross-module contract primitives: `IIntegrationEvent`, shared identifiers. |
| `BSX.BuildingBlocks` | CQRS, behaviors, persistence and authorization abstractions. |

## Dependency Direction

```mermaid
flowchart TD
    DOMAIN["Module.Domain"]
    APP["Module.Application"]
    INFRA["Module.Infrastructure"]
    EP["Module.Endpoints"]
    CONTRACTS["Module.Contracts"]
    SK["BSX.SharedKernel"]
    CON["BSX.Contracts"]
    BB["BSX.BuildingBlocks"]
    OTHER["Other modules"]

    DOMAIN --> SK
    APP --> DOMAIN
    APP --> CONTRACTS
    APP --> BB
    APP --> SK
    INFRA --> APP
    INFRA --> DOMAIN
    EP --> APP
    CONTRACTS --> CON
    CON --> SK
    BB --> CON
    OTHER -->|"references only"| CONTRACTS
```

Rules:

- `Domain` depends on the SharedKernel only. No EF Core, no BuildingBlocks, no other module.
- `Application` depends on Domain, its own Contracts, BuildingBlocks and the SharedKernel. It
  defines ports; it does not implement infrastructure.
- `Infrastructure` depends on Application and Domain and owns EF Core.
- `Endpoints` depends on Application and maps `Result` to transport.
- `Contracts` depends on `BSX.Contracts` only (ADR-015). It carries integration events, integration
  DTOs and shared identifiers; it never references a module's Domain, Application or
  Infrastructure.
- Other modules reference **only** `Module.Contracts`. Direct references to another module's
  Domain/Application/Infrastructure are forbidden.
- Inter-module reactions use integration events (ADR-009), never direct calls.

## Composition

```mermaid
flowchart LR
    HOST["BSX.Web<br/>(composition root)"] --> M1[IModule CRM]
    HOST --> M2[IModule Sales]
    HOST --> M3[IModule ...]
    M1 --> REG["RegisterServices()<br/>MapEndpoints()"]
    M2 --> REG
    M3 --> REG
```

Each module exposes a single `IModule` entry point that registers its services, handlers,
validators, configurations and endpoints. The host composes modules without knowing their
internals. Solution folders `src/Modules` and `src/Platform` hold the modules until implemented.

See [ADR-015](../decisions/ADR-015-Contracts-Abstraction-Strategy.md) for the contracts
abstraction rationale.
