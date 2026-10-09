# BSX Platform CQRS Skill

## Purpose

Defines how to implement commands, queries, handlers, validators and endpoints in BSX Platform.

Read this before implementing any vertical slice.

This document is a summary. The ADRs and architecture documents are the source of truth.

---

## Source of Truth

- ADR-002 — CQRS
- ADR-007 — In-Process CQRS Dispatcher
- ADR-009 — Domain Event Dispatching Strategy
- ADR-011 — Result and Error Mapping Strategy
- ADR-012 — Authorization Architecture
- `docs/diagrams/cqrs-pipeline.md`
- `docs/architecture/implementation-guidelines.md`

---

## Core Rules

- Use CQRS (Rule 3): commands modify state, queries read state. Never mix responsibilities.
- Use Vertical Slice (Rule 4): one request per operation, colocated in the module's Application
  layer.
- Every request returns `Result` or `Result<T>` (ADR-011). Expected failures are values, not
  exceptions.
- Dispatch only through `ISender`. Never call a handler directly.

---

## Commands

- `ICommand` / `ICommand<T>` with an `ICommandHandler`.
- Modify state through aggregates. Business logic stays in the Domain (`ddd-skill.md`).
- One transaction per command. Never manage transactions manually — the `TransactionBehavior` and
  `IUnitOfWork` own them (`docs/architecture/implementation-guidelines.md`).
- Raise domain events from aggregates; persistence persists and dispatches them (ADR-009).

---

## Queries

- `IQuery<T>` with an `IQueryHandler`.
- Read-only: never modify state and never start a transaction.
- Project directly to DTOs; never load full aggregates for read screens (database-skill).
- Do not use aggregate repositories for reporting.

---

## Handlers

- One handler per request; sealed; named `<Verb><Subject>Handler`.
- Methods are `*Async` and accept a `CancellationToken`.
- Depend on abstractions: aggregate-specific repositories, `IUnitOfWork`, `ICurrentUser`.

---

## Pipeline

Order is fixed: **Logging → Validation → Authorization → Transaction → Handler**
(`docs/diagrams/cqrs-pipeline.md`).

- Validation: FluentValidation runs before the handler and produces structured, field-level errors
  (ADR-011).
- Authorization: declarative `[RequirePermission]`; check permissions, not roles (ADR-012).
- Transaction: commands only.
- Do not reorder or bypass behaviors without an ADR.

---

## Endpoints

- Thin transport adapters; no business logic (Rule 2).
- Map `Result` to transport through `IErrorMapper`; never serialize `Result` to the wire (ADR-011).

---

## Persistence Access

- Repositories are aggregate-specific (Rule 6). `IRepository<T>` is forbidden.
- Commands load and persist aggregates; queries use projections.

---

## Testing

- Unit test handler logic and the business rules it invokes.
- Integration test handlers against real infrastructure where possible.
- Cover validation, authorization and failure paths.

---

## Checklist

- [ ] Request returns `Result`.
- [ ] Handler is single, sealed and async.
- [ ] Validator exists and returns structured errors.
- [ ] Permission declared if the operation is protected.
- [ ] Business logic lives in the Domain.
- [ ] Query projects to DTOs and opens no transaction.
- [ ] Domain events raised for meaningful occurrences.
- [ ] Endpoint is thin and maps `Result`.
