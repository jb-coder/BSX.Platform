# ADR-001 - Modular Monolith Architecture

## Status

Accepted

---

## Context

BSX Platform requires strong modularity while maintaining development velocity and operational simplicity.

Microservices introduce unnecessary complexity for the current stage of the project.

---

## Decision

Adopt a Modular Monolith architecture.

The application will be deployed as a single unit while enforcing module boundaries internally.

Modules:

- Identity
- CRM
- Sales
- Inventory
- Purchasing
- Accounting
- Notifications

---

## Consequences

### Positive

- Simpler deployments
- Faster development
- Easier debugging
- Lower infrastructure costs

### Negative

- Single deployment unit
- Requires discipline to maintain boundaries

---

## Notes

Future extraction to microservices remains possible through Domain Events and the Outbox Pattern.