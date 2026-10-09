# ADR-006 - Shared Kernel and Building Blocks Separation

## Status

Accepted

---

## Context

The platform requires reusable domain primitives and reusable technical patterns. Placing both
in a single shared project would slowly turn it into a dumping ground and would couple the domain
model to technical infrastructure concerns.

---

## Decision

Separate the two concerns into distinct projects:

- `BSX.SharedKernel` — domain primitives only, zero dependencies, database agnostic.
- `BSX.BuildingBlocks` — technical cross-cutting abstractions; depends on the Shared Kernel only.

SharedKernel may contain Entity, AggregateRoot, ValueObject, DomainEvent, Result and Error.
No business concepts are allowed in either project.

---

## Alternatives Considered

### Single shared project

Pros

- Fewer projects.

Cons

- Mixes domain and infrastructure concerns.
- Breaks the dependency direction rule.
- Becomes a dumping ground.

### No shared kernel

Pros

- Maximum isolation per module.

Cons

- Duplicated domain primitives across modules.
- Duplicated business rules, which the development principles forbid.

---

## Consequences

### Positive

- Clear dependency direction.
- Domain stays independent from infrastructure.
- Enforced by architecture tests.

### Negative

- Two projects to maintain instead of one.

---

## Notes

The Shared Kernel must remain small. Anything that is not a fundamental domain primitive belongs
in BuildingBlocks or in a module.
