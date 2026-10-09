# Implementation Guidelines

Mandatory conventions for every module. These rules operationalize the architecture skills and
ADRs 006–012. Deviation requires an ADR.

---

## 1. Module Structure

```text
src/Modules/<Module>
├── <Module>.Domain
├── <Module>.Application
├── <Module>.Infrastructure
├── <Module>.Contracts
└── <Module>.Endpoints
```

| Project | May reference |
| --- | --- |
| `<Module>.Domain` | `BSX.SharedKernel` only |
| `<Module>.Application` | `<Module>.Domain`, `BSX.BuildingBlocks`, `BSX.SharedKernel` |
| `<Module>.Infrastructure` | `<Module>.Application`, `<Module>.Domain` |
| `<Module>.Endpoints` | `<Module>.Application` |
| `<Module>.Contracts` | `BSX.SharedKernel` |

- `Domain` never references EF Core, BuildingBlocks or another module.
- Other modules reference only `<Module>.Contracts`.
- See [module-structure.md](../diagrams/module-structure.md).

---

## 2. Module Composition

- Every module exposes exactly one `IModule` implementation with:
  - `RegisterServices(IServiceCollection, IConfiguration)`
  - `MapEndpoints(IEndpointRouteBuilder)`
- `RegisterServices` calls `AddBuildingBlocks(typeof(<Module>Endpoints).Assembly)`.
- The host composes modules through `IModule` and references no module internals.
- Modules add their projects under the `src/Modules` solution folder.

---

## 3. CQRS Conventions

- One request per operation, one handler per request, in the module's `Application` layer.
- Commands modify state and return `Result` or `Result<T>`. Queries return `Result<T>`.
- Handlers implement `ICommandHandler` / `IQueryHandler`.
- Names: `<Verb><Subject>Command`, `<Verb><Subject>Query`, `<Verb><Subject>Handler`.
- Methods are `*Async` and accept a `CancellationToken`.
- A query never modifies state and never opens a transaction.
- One handler per request; no duplicate handlers.

Pipeline order (fixed): **Logging → Validation → Authorization → Transaction → Handler**.

---

## 4. Transactions

- Only commands run inside transactions, opened and committed by `TransactionBehavior` through
  `IUnitOfWork`.
- Commands must not manage transactions manually.
- Multi-step commands use a single unit-of-work commit; avoid partial saves.
- Domain events are persisted in the same transaction and dispatched after commit (ADR-009).

---

## 5. Domain Events

- Aggregates raise domain events during command execution.
- Persistence collects and persists them atomically; dispatch happens after commit (ADR-009).
- Domain events never cross module boundaries.
- Domain handlers live in the owning module, are idempotent, and must not depend on another
  module internally.
- To react across modules, publish an integration event through the Outbox.
- A default `IEventBus` implementation is always registered (it persists to the Outbox); modules
  never resolve an unregistered bus.

---

## 6. Validation

- Use FluentValidation; validators live next to the request.
- Validation runs before authorization and the handler.
- Failures produce structured, field-level `Error`s (`Kind = Validation`); never concatenated
  strings.
- Domain invariants are enforced inside aggregates using the Result pattern, not validators.

---

## 7. Results and Errors

- Return `Result` / `Result<T>`; never throw for expected failures.
- Error `Code` format: `MODULE.Subject.Problem` (for example, `CRM.Customer.EmailAlreadyExists`).
  Codes are stable and never change once published.
- `Message` is localizable; never build code from message text.
- `ErrorKind` is semantic (`Failure`, `Validation`, `NotFound`, `Conflict`, `Authentication`,
  `Permission`, `Unexpected`); never map it to HTTP inside a module.
- Transport mapping happens only in `Endpoints` via `IErrorMapper`.
- Do not leak `Result` to the wire; endpoints map it.

---

## 8. Authorization

- Declare a permission with `[RequirePermission("resource.action")]` on the request type.
- Check permissions, not roles.
- Resource-level rules (ownership, tenant, entity state) are evaluated in the handler using
  `ICurrentUser`.
- Authorization failures return `ErrorKind.Authentication` (401) or `ErrorKind.Permission` (403).
- Never allow-all by default; the platform checker is fail-closed until Identity replaces it.

---

## 9. Auditing and Soft Delete

- Never add audit fields to domain entities.
- Auditing is automatic through the persistence pipeline (ADR-010).
- Soft delete is the default for business data; hard delete requires justification and is limited
  to infrastructure data.
- Do not expose audit fields to the domain; model business lifecycle explicitly when needed.

---

## 10. Persistence

- EF Core, Code First, PostgreSQL; migrations tracked in source control.
- Every aggregate has its own configuration; avoid one large configuration file.
- Repositories are aggregate-specific (`ICustomerRepository`); `IRepository<T>` is forbidden.
- Read screens project to DTOs; never load full aggregates for reporting.
- Create indexes intentionally (business keys, foreign keys, filters).
- Migration naming: `Add<Module><Subject>` and meaningful.
- Multi-tenancy: reserve `TenantId`; do not implement tenancy yet.

---

## 11. Observability

- Structured logging only (Serilog); no string concatenation.
- Include `CorrelationId`, `EventId`, `TraceId`/`SpanId` where applicable.
- Never log payloads containing personal data.
- Add counters/histograms for business-critical operations.

---

## 12. Testing

- Unit tests validate business rules (`Should_..._When...`), using FluentAssertions.
- Integration tests validate infrastructure against real dependencies where possible.
- Architecture tests are mandatory and evolve with the rules.
- Prefer confidence over coverage percentages.

---

## 13. Checklist

Before a slice is complete:

- [ ] Request returns `Result`; no exceptions for expected failures.
- [ ] Validator exists and produces structured errors.
- [ ] Permission declared if protected.
- [ ] Business rules live in the aggregate.
- [ ] Domain events raised for meaningful occurrences.
- [ ] No cross-module direct references; contracts only.
- [ ] Repository is aggregate-specific.
- [ ] Queries project to DTOs and start no transaction.
- [ ] Auditing is automatic; no audit fields in the domain.
- [ ] Error codes are stable and namespaced.
- [ ] Unit tests cover the business paths.
