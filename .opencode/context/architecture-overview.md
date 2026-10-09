# BSX Platform Architecture Overview

## Architecture Style

Modular Monolith

The application is built as a single deployable unit while maintaining strong module boundaries.

---

## Backend

- ASP.NET Core .NET 10
- Entity Framework Core
- PostgreSQL

---

## Frontend

- Blazor Web App
- MudBlazor
- Tailwind CSS

---

## Architectural Patterns

- Domain Driven Design
- CQRS
- Vertical Slice Architecture
- Domain Events
- Result Pattern

---

## Communication Strategy

Modules must communicate through:

- Domain Events
- Contracts

Direct dependency between modules is discouraged.

---

## Deployment

Docker-first strategy.

Future SaaS deployment must remain possible without architectural changes.

---

## Observability

- Serilog
- OpenTelemetry

---

## Testing

- Unit Tests
- Integration Tests
- Architecture Tests
- End-to-End Tests