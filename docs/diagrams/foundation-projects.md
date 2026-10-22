# Foundation Projects

Project dependency graph of the technical foundation.

```mermaid
flowchart TD

    Web["BSX.Web<br/>(Blazor Web App - composition root)"]

    Persistence["BSX.Persistence<br/>(EF Core outbox - interceptor - store - recovery)"]

    BuildingBlocks["BSX.BuildingBlocks<br/>(CQRS - behaviors - persistence abstractions - diagnostics - rate limiting)"]

    Contracts["BSX.Contracts<br/>(integration-event primitives - permission catalog)"]

    SharedKernel["BSX.SharedKernel<br/>(Entity - AggregateRoot - ValueObject - DomainEvent - Result)"]

    Modules["Modules<br/>(Domain - Application - Infrastructure - Contracts - Endpoints)"]

    Web --> BuildingBlocks
    Web --> Contracts
    Web --> Persistence
    Web --> Modules
    Persistence --> BuildingBlocks
    Persistence --> Contracts
    Persistence --> SharedKernel
    BuildingBlocks --> SharedKernel
    BuildingBlocks --> Contracts
    Contracts --> SharedKernel
    Modules --> BuildingBlocks
    Modules --> Contracts
    Modules --> Persistence
```

Rules enforced by architecture tests:

- `BSX.SharedKernel` depends on nothing external.
- `BSX.Contracts` owns cross-module contract primitives and the permission catalog.
- `BSX.BuildingBlocks` depends on `BSX.SharedKernel` and `BSX.Contracts`; it never references EF Core.
- `BSX.Persistence` depends on the platform projects and EF Core; it never references a module.
- Modules depend on the kernel, contracts, building blocks and persistence; a module never
  references another module's implementation (only its Contracts).
