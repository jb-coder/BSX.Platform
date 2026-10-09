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
