# ADR-011 - Result and Error Mapping Strategy

## Status

Accepted

---

## Context

The architecture mandates the Result Pattern (Rule 5): expected failures are values, not
exceptions.

The foundation review identified three High/Medium issues:

1. `ErrorType` in `BSX.SharedKernel.Results` carried transport-flavored semantics
   (`Unauthorized`, `Forbidden`, `Conflict`, `NotFound`) inside the domain kernel, with no
   mapping layer separating semantics from transport.
2. Validation failures were flattened into a single concatenated message, losing field-level
   detail required by any real API.
3. A `Result` carried a single `Error`, preventing the natural aggregation of multiple failures.

The platform also needs a consistent way to surface results to HTTP (Blazor endpoints), and
later to other transports, without letting transport concerns leak into the kernel or modules.

---

## Decision

The kernel defines **error semantics**; the transport adapter defines **error mapping**. The two
never mix.

### 1. Error model

- `Error` carries:
  - `Code` — a stable, namespaced, machine-readable code (for example,
    `CRM.Customer.EmailAlreadyExists`).
  - `Message` — a human-readable, localizable message.
  - `Kind` — a coarse, **presentation-agnostic** classification.
  - `Field` — an optional target (input name) used for validation errors.
- `ErrorKind` replaces `ErrorType`. It is explicitly documented as a semantic category, **not**
  an HTTP status. Members:
  `Failure`, `Validation`, `NotFound`, `Conflict`, `Authentication`, `Permission`, `Unexpected`.
- The kernel contains no HTTP types, no status codes and no `ProblemDetails`.

### 2. Result aggregates errors

- `Result` and `Result<TValue>` expose `Errors` as a read-only collection.
- Success carries zero errors; failure carries one or more.
- This allows a single operation to return multiple validation or domain failures at once.

### 3. Validation errors are structured

- FluentValidation failures become one `Error` per failure, each with `Kind = Validation`,
  `Field = property name`, a stable `Code`, and a message.
- `ValidationBehavior` builds the structured `Result`; it never concatenates messages.
- A validation result is therefore directly serializable to a field-keyed error dictionary.

### 4. Exceptions vs results

- Expected failures (validation, not found, conflict, authentication, permission) return
  `Result`; they never throw.
- Exceptions are reserved for programmer errors, broken invariants and genuinely unexpected
  faults. The host's global exception handler maps these to `ErrorKind.Unexpected`.

### 5. Error catalog

- Each module owns an error catalog (`Errors`) of stable codes and messages.
- Codes are namespaced by module and never change once published.
- Messages are resolved from resources to remain localization-ready.
- Domain errors use domain codes; application errors use application codes; both share the same
  `Error` type.

### 6. Transport mapping (adapter only)

- Mapping lives exclusively in the host/adapter, behind an `IErrorMapper`.
- Default HTTP mapping (RFC 9457 ProblemDetails):

| ErrorKind | HTTP status |
| --- | --- |
| `Validation` | 400 |
| `Authentication` | 401 |
| `Permission` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` | 422 |
| `Unexpected` | 500 |

- `Validation` errors populate the ProblemDetails `errors` dictionary keyed by `Field`, each
  entry holding `(code, message)`.
- `Result` never crosses the wire directly; endpoints map it before returning.
- Blazor and any future transport implement their own mapping; the kernel and modules are
  unaffected.

### 7. Combinators

- `Map`, `Bind`, `Match`, `Tap` are provided as extension methods in `BSX.BuildingBlocks`
  (Application layer), **not** in the kernel. The kernel stays minimal (Rule 9).

---

## Alternatives Considered

### Keep `ErrorType` with transport names in the kernel

Pros

- No migration.

Cons

- Kernel knows presentation outcomes; violates domain agnosticism.
- No mapping layer; transport decisions leak everywhere.

### Single error per Result

Pros

- Simplest.

Cons

- Cannot express multiple validation failures; forces concatenation or exceptions.

### A dedicated result library (Ardalis.Result / FluentResults)

Pros

- Feature-rich.

Cons

- External dependency and governance cost (consistent with ADR-007).
- A small, owned model is sufficient.

### `HttpStatusCode` carried on `Error`

Pros

- Direct.

Cons

- Couples the kernel to ASP.NET Core.
- Blocks non-HTTP transports.

---

## Consequences

### Positive

- The kernel is transport-agnostic.
- APIs can return precise, field-level, localized errors.
- One mapping point for all HTTP responses.
- Modules share a single, predictable error vocabulary.

### Negative

- Existing `ErrorType` usages must be migrated to `ErrorKind`.
- `Result` gains an error collection and loses single-error convenience; factories must cover the
  common cases.

---

## Notes

- Referenced by: [ADR-012](ADR-012-Authorization-Architecture.md),
  [error-mapping.md](../diagrams/error-mapping.md),
  [shared-kernel.md](../architecture/shared-kernel.md),
  [implementation-guidelines.md](../architecture/implementation-guidelines.md).
