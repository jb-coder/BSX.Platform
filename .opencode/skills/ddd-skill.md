# BSX Platform Domain-Driven Design Skill

## Purpose

Defines how to model the domain in BSX Platform.

Read this before designing or changing aggregates, entities or value objects.

This document is a summary. The ADRs and architecture documents are the source of truth.

---

## Source of Truth

- ADR-003 — Domain Driven Design
- ADR-009 — Domain Event Dispatching Strategy
- ADR-010 — Auditing Strategy
- ADR-011 — Result and Error Mapping Strategy
- `docs/architecture/shared-kernel.md`
- `docs/architecture/implementation-guidelines.md`
- `docs/diagrams/module-structure.md`

---

## Scope

Domain layer only.

Business logic belongs to the Domain and nowhere else — not in endpoints, handlers, repositories
or infrastructure (architecture Rule 2).

---

## Building Blocks

Use the SharedKernel primitives exactly as defined in `docs/architecture/shared-kernel.md`:

- `Entity<TId>`
- `AggregateRoot<TId>`
- `ValueObject`
- `DomainEvent`
- `Result` / `Error`

Do not add new primitives to the kernel (Rule 9).

---

## Modeling Rules

### Aggregates

- One aggregate is one consistency boundary and one transaction.
- The aggregate root is the only entry point; external code never touches inner entities.
- Reference other aggregates by identity only, never by navigation graph.
- Keep aggregates small; split them when they grow. God aggregates are forbidden.
- Enforce every invariant inside the aggregate.

### Identity

- Use strongly typed identifiers (`CustomerId`, `OrderId`), never primitive strings or GUIDs in
  signatures. No primitive obsession (database-skill).
- Define identifiers as value objects inside the module; do not add them to the kernel.

### Value Objects

- Immutable, no identity, equality by components.
- Prefer value objects over primitive fields for meaningful concepts (`Money`, `Email`,
  `Address`, `CustomerNumber`).

### Behavior

- Put behavior with data. Anemic models are forbidden.
- Return `Result` for expected failures; never throw for business rule violations (ADR-011).
- Do not expose state setters; mutate through intention-revealing methods.

### Domain Events

- Only aggregate roots raise domain events.
- Domain events are intra-module and never cross a module boundary (ADR-009).
- Name them in the past tense for a meaningful occurrence (`CustomerCreated`, `OrderSubmitted`).
- Cross-module reactions use integration events, never domain events (ADR-009).

### Persistence Independence

- The Domain references `BSX.SharedKernel` only. No EF Core, no `BSX.BuildingBlocks`, no other
  module (`docs/diagrams/module-structure.md`).
- No persistence attributes; configuration lives in Infrastructure (database-skill).
- Auditing and soft delete are persistence concerns applied through shadow properties. Never add
  audit fields to domain entities (ADR-010).
- A business-visible lifecycle value (for example `RegisteredOn`) is explicit domain state and is
  never the technical audit column (ADR-010).

---

## Modeling Checklist

- [ ] Invariants enforced inside the aggregate.
- [ ] Behavior, not a data holder.
- [ ] Strongly typed identity.
- [ ] Cross-aggregate references by id only.
- [ ] Domain events raised for meaningful occurrences.
- [ ] No infrastructure or other-module references.
- [ ] Expected failures returned as `Result`.
