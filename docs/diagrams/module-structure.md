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

## Dependency Direction

```mermaid
flowchart TD
    DOMAIN["Module.Domain"]
    APP["Module.Application"]
    INFRA["Module.Infrastructure"]
    EP["Module.Endpoints"]
    CONTRACTS["Module.Contracts"]
    SK[BSX.SharedKernel]
    BB[BSX.BuildingBlocks]
    OTHER[Other modules]

    DOMAIN --> SK
    APP --> DOMAIN
    APP --> BB
    APP --> SK
    INFRA --> APP
    INFRA --> DOMAIN
    EP --> APP
    CONTRACTS --> SK
    OTHER -->|"references only"| CONTRACTS
```

Rules:

- `Domain` depends on the SharedKernel only. No EF Core, no BuildingBlocks, no other module.
- `Application` depends on Domain and BuildingBlocks. It defines ports; it does not implement
  infrastructure.
- `Infrastructure` depends on Application and Domain and owns EF Core.
- `Endpoints` depends on Application and maps `Result` to transport.
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
