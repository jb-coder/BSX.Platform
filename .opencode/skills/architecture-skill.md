# BSX Platform Architecture Skill

## Purpose

This document defines the architectural rules of BSX Platform.

Every implementation must comply with these rules.

---

# Rule 1

Modules are more important than layers.

Protect module boundaries at all times.

---

# Rule 2

Business logic belongs to the Domain layer.

Never place business rules inside:

- Controllers
- Endpoints
- Repositories
- Infrastructure

---

# Rule 3

Use CQRS.

Commands modify state.

Queries read state.

Do not mix responsibilities.

---

# Rule 4

Use Vertical Slice Architecture.

Prefer:

Customer
 └─ CreateCustomer

instead of:

Commands
Queries
Handlers

distributed across large shared folders.

---

# Rule 5

Use Result Pattern.

Business errors should not rely on exceptions.

Expected failures must return Result objects.

---

# Rule 6

Repositories are Aggregate specific.

Allowed:

ICustomerRepository

IOrderRepository

Forbidden:

IRepository<T>

---

# Rule 7

Modules communicate through events.

Examples:

CustomerCreated

OrderCreated

InvoiceGenerated

---

# Rule 8

Avoid premature abstractions.

Do not introduce new layers without a clear need.

---

# Rule 9

SharedKernel must remain small.

Only place:

- Entity
- AggregateRoot
- ValueObject
- DomainEvent
- Result

inside SharedKernel.

---

# Rule 10

Architectural simplicity is preferred over architectural purity.

When in doubt:

Choose the simpler solution.