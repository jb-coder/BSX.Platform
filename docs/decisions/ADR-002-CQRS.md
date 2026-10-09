# ADR-002 - CQRS

## Status

Accepted

---

## Context

The platform requires clear separation between write operations and read operations.

---

## Decision

Adopt CQRS across all business modules.

Commands:

- Modify state

Queries:

- Read state

---

## Consequences

### Positive

- Clear separation of responsibilities
- Better scalability
- Easier maintenance

### Negative

- Increased number of files
- Requires architectural discipline

---

## Notes

CQRS will be combined with Vertical Slice Architecture.
