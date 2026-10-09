# Foundation Projects

Project dependency graph of the technical foundation.

```mermaid
flowchart TD

    Web["BSX.Web<br/>(Blazor Web App · composition root)"]

    BuildingBlocks["BSX.BuildingBlocks<br/>(CQRS · events · persistence abstractions)"]

    SharedKernel["BSX.SharedKernel<br/>(Entity · AggregateRoot · ValueObject · DomainEvent · Result)"]

    Web --> BuildingBlocks
    BuildingBlocks --> SharedKernel
```

Rules enforced by architecture tests:

- `BSX.SharedKernel` depends on nothing external.
- `BSX.BuildingBlocks` depends on `BSX.SharedKernel` only.
- `BSX.Web` depends on `BSX.BuildingBlocks` and `BSX.SharedKernel`.
- Modules (future) depend on the kernel and the building blocks, never on each other.
