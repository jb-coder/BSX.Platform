# ADR-007 - In-Process CQRS Dispatcher

## Status

Accepted

---

## Context

The architecture mandates CQRS and pipeline behaviors (validation, logging). A mediator is
required to dispatch commands and queries to their handlers.

The skills define the CQRS abstractions but do not name a mediator library. Adding an external
mediator introduces a dependency whose licensing may change and whose abstractions would leak
into every module.

---

## Decision

Implement a small in-process dispatcher inside `BSX.BuildingBlocks`.

- `ISender` receives a request and returns its response.
- A cached wrapper resolves the handler and the registered `IPipelineBehavior` instances.
- Reflection is performed once per request type and cached.
- Requests return `Result` or `Result<TValue>`.

No external mediator library is used.

---

## Alternatives Considered

### External mediator (e.g. MediatR)

Pros

- Battle-tested.
- Rich ecosystem.

Cons

- Commercial licensing for recent versions.
- Abstractions leak into every module.
- Violates technology governance: recommend only when it provides measurable value.

### Abstractions only, dispatch later

Pros

- Less code now.

Cons

- Blocks the first module.
- Behaviors could not be implemented or tested.

---

## Consequences

### Positive

- No external dependency or license risk.
- Full control over the pipeline.
- Kernel-level unit tests cover dispatch and behaviors.

### Negative

- The dispatcher must be maintained by the platform team.

---

## Notes

`AddBuildingBlocks(params Assembly[])` scans the supplied assemblies and registers handlers,
domain event handlers and validators.
