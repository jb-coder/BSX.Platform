# Module Boundaries

## Principle

Each module represents an independent business capability.

Modules must not depend directly on each other's implementation.

---

## Allowed Communication

### Domain Events

Example:

CustomerCreated

OrderSubmitted

InvoiceGenerated

---

### Contracts

Explicit integration contracts.

---

## Forbidden Communication

Direct access to:

- Infrastructure layer
- Database tables
- Internal services

of another module.

---

## Goal

Maintain low coupling and high cohesion.