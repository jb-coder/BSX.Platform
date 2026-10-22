# Module Boundaries

## Principle

Each module represents an independent business capability.

Modules must not depend directly on each other's implementation.

---

## Allowed Communication

### Domain Events (intra-module)

Domain events are internal to a module. They never cross a module boundary.

Example:

CustomerCreated

OrderSubmitted

InvoiceGenerated

See [ADR-009](../decisions/ADR-009-Domain-Event-Dispatching-Strategy.md).

---

### Integration Events (inter-module)

To cross a boundary, a domain event is translated into one or more integration events, which
travel through the Outbox and a message broker.

Integration events are published, versioned contracts.

---

### Contracts

Each module exposes a `<Module>.Contracts` project. It is the **only** project other modules may
reference. It contains integration events and integration DTOs, never domain or infrastructure
types.

`<Module>.Contracts` references `BSX.Contracts`, which owns the cross-module contract primitives
(`IIntegrationEvent`, the integration-event base and shared identifiers). See
[ADR-015](../decisions/ADR-015-Contracts-Abstraction-Strategy.md).

---

## Forbidden Communication

Direct access to:

- Another module's Domain, Application or Infrastructure projects
- Database tables or schemas owned by another module
- Internal services of another module

---

## Composition

- Every module exposes a single `IModule` entry point with `RegisterServices` and `MapEndpoints`.
- The host (`BSX.Web`) composes modules without knowing their internals.
- See [module-structure.md](../diagrams/module-structure.md) for the internal layout and
  dependency direction.

---

## Goal

Maintain low coupling and high cohesion.

---

## Enforcement

`tests/BSX.ArchitectureTests` is the executable form of these rules:

- Domain assemblies cannot reference BuildingBlocks, Persistence, EF Core, FluentValidation or
  another module.
- Contracts assemblies can reference only `BSX.Contracts`; they cannot reference implementation
  layers or another module.
- Application cannot reference its own Infrastructure/Endpoints or another module's implementation.
- Endpoints cannot reference their own Domain or another module's implementation.
- Infrastructure can reference another module only through that module's Contracts.
- Every `[RequirePermission]` code in an Application assembly must be declared by a permission
  catalog.
