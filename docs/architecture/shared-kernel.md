# Shared Kernel

The Shared Kernel must remain extremely small.

Project: `src/BSX.SharedKernel`
Root namespace: `BSX.SharedKernel`

---

## Contents

Primitives (`BSX.SharedKernel.Primitives`):

- `Entity<TId>`
- `AggregateRoot<TId>`
- `IHasDomainEvents` (non-generic marker required by the persistence layer — ADR-009)
- `ValueObject`
- `IDomainEvent`
- `DomainEvent`

Results (`BSX.SharedKernel.Results`):

- `Result`
- `Result<TValue>`
- `Error`
- `ErrorKind`

---

## Result and Error

- `Error` carries a stable namespaced `Code`, a localized `Message`, a semantic `ErrorKind`, and
  an optional `Field` for validation errors.
- `ErrorKind` is a **semantic category**, not an HTTP status. The kernel contains no HTTP types.
- `Result` and `Result<TValue>` expose an `Errors` collection (zero on success, one or more on
  failure).
- Validation failures are structured, one `Error` per field failure (see ADR-011).

---

## Rules

- Zero external dependencies.
- Database agnostic. No persistence, serialization or transport concerns.
- No transport mapping. HTTP status mapping lives in the host adapter.
- No business rules. Business concepts belong to modules.
- Everything else should remain outside of SharedKernel.

SharedKernel is not a dumping ground.
