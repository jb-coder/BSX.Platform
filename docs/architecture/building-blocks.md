# Building Blocks

Contains reusable technical patterns.

Project: `src/BSX.BuildingBlocks`
Root namespace: `BSX.BuildingBlocks`

Business concepts are forbidden here.

---

## CQRS

Namespace: `BSX.BuildingBlocks.Cqrs`

- `IRequest<TResponse>`
- `ICommand` / `ICommand<TResponse>`
- `IQuery<TResponse>`
- `IRequestHandler<TRequest, TResponse>`
- `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>`
- `IQueryHandler<TQuery, TResponse>`
- `IPipelineBehavior<TRequest, TResponse>`
- `RequestHandlerDelegate<TResponse>`
- `ISender` and its in-process implementation

Requests return `Result` or `Result<TValue>`. Failures are values, not exceptions.

---

## Behaviors

Namespace: `BSX.BuildingBlocks.Behaviors`

Canonical order (outermost first):

1. `LoggingBehavior` — structured start/completion logging.
2. `ValidationBehavior` — FluentValidation; short-circuits with structured validation errors.
3. `AuthorizationBehavior` — permission gate for requests that declare a requirement.
4. `TransactionBehavior` — opens a transaction for commands only.

See [cqrs-pipeline.md](../diagrams/cqrs-pipeline.md).

---

## Events

Namespace: `BSX.BuildingBlocks.Events`

- `IDomainEventDispatcher` and its in-process implementation
- `IDomainEventHandler<TDomainEvent>`
- Outbox publishing of integration events

The integration-event contract primitives (`IIntegrationEvent` and the integration-event base)
live in `BSX.Contracts`, so module Contracts can reference them without depending on
BuildingBlocks. `IEventBus` remains the publishing abstraction. See
[ADR-015](../decisions/ADR-015-Contracts-Abstraction-Strategy.md).

Domain events are internal to a module and dispatched in-process after commit. Integration events
are the only inter-module channel and travel through the Outbox. See
[ADR-009](../decisions/ADR-009-Domain-Event-Dispatching-Strategy.md).

A default `IEventBus` implementation is always registered (it writes to the Outbox) so resolving
it never fails before a broker is configured.

---

## Persistence

Namespace: `BSX.BuildingBlocks.Persistence`

- `IUnitOfWork`
- `IDatabaseTransaction`

Auditing and soft delete are **not** contracts. They are applied by the persistence layer through
EF Core shadow properties and a centralized interceptor. Domain entities do not implement
auditing interfaces. See [ADR-010](../decisions/ADR-010-Auditing-Strategy.md).

---

## Authorization

Namespace: `BSX.BuildingBlocks.Authorization`

- `ICurrentUser`
- `IPermissionChecker`
- `RequirePermissionAttribute`

Defaults are always registered: `ICurrentUser` is anonymous, `IPermissionChecker` is
fail-closed. See [ADR-012](../decisions/ADR-012-Authorization-Architecture.md).

---

## Composition

Namespace: `BSX.BuildingBlocks.Modules`

- `IModule` — the single entry point every module exposes (`RegisterServices`, `MapEndpoints`).

The host composes modules through this abstraction and never references module internals. See
[module-structure.md](../diagrams/module-structure.md).

---

## Registration

Namespace: `BSX.BuildingBlocks.DependencyInjection`

- `AddBuildingBlocks(params Assembly[])` registers the dispatchers, the canonical behaviors, the
  default authorization services, validators and handlers found in the supplied assemblies.

Modules register themselves through `IModule`, passing their assembly to `AddBuildingBlocks`.
