# ADR-004 - PostgreSQL

## Status

Accepted

---

## Context

The platform requires a reliable, open-source relational database.

---

## Decision

Use PostgreSQL as the primary database engine.

---

## Consequences

### Positive

- Mature ecosystem
- Excellent performance
- No licensing costs

### Negative

- Team must understand PostgreSQL-specific behaviors

---

## Notes

Persistence access will be implemented using Entity Framework Core.