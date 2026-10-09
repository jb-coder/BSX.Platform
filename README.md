# BSX Platform

> The Core of Modern Business.

BSX Platform is a modern, modular, and scalable business operating system built with .NET 10 and Blazor.

The platform is designed to unify core business capabilities such as CRM, Sales, Inventory, Accounting, Purchasing, Human Resources, and future modules within a single enterprise-grade ecosystem.

Built with a strong focus on maintainability, extensibility, and long-term evolution, BSX Platform follows modern software architecture principles including Modular Monolith, Domain-Driven Design (DDD), CQRS, Vertical Slice Architecture, and Clean Architecture.

---

## Vision

BSX Platform aims to become a flexible and configurable business platform capable of adapting to different industries and company sizes while maintaining a clean and scalable foundation.

Core principles:

- Scalability
- Modularity
- Simplicity
- Developer Experience
- Long-Term Maintainability
- Modern User Experience

---

## Technology Stack

### Frontend

- Blazor Web App (.NET 10)
- MudBlazor
- Tailwind CSS

### Backend

- ASP.NET Core (.NET 10)
- Entity Framework Core
- PostgreSQL

### Architecture

- Modular Monolith
- Domain-Driven Design (DDD)
- CQRS
- Vertical Slice Architecture
- Domain Events
- Result Pattern
- Clean Architecture (per module)

### DevOps

- Docker
- GitFlow
- GitHub Actions
- Semantic Versioning

### Observability

- Serilog
- OpenTelemetry

### Testing

- xUnit
- FluentAssertions

---

## Project Structure

```text
BSX.Platform
│
├── docs
├── infrastructure
├── src
├── tests
└── .opencode
```

### Source Code

```text
src
│
├── BSX.Web
├── BSX.SharedKernel
├── BSX.BuildingBlocks
│
├── Modules
│   ├── Identity
│   ├── CRM
│   ├── Sales
│   ├── Inventory
│   ├── Purchasing
│   └── Accounting
│
└── Platform
    ├── Notifications
    ├── Storage
    ├── BackgroundJobs
    └── Observability
```

---

## Architectural Principles

### Modular Monolith

The system is organized into independent modules with explicit boundaries.

### Domain-Driven Design

Business rules and domain knowledge are encapsulated within each module.

### CQRS

Commands and Queries are separated to improve maintainability and scalability.

### Vertical Slice Architecture

Features are organized around business capabilities rather than technical layers.

### Domain Events

Modules communicate through events to maintain loose coupling.

---

## Documentation

All architecture decisions, diagrams, conventions, and implementation guides are stored in the `docs` folder.

Key documentation includes:

- Architecture Overview
- ADRs (Architecture Decision Records)
- Module Specifications
- Development Guidelines
- Deployment Guides
- Roadmap
- Mermaid Diagrams

---

## Git Workflow

BSX Platform follows GitFlow.

### Main Branches

```text
main
develop
```

### Supporting Branches

```text
feature/*
release/*
hotfix/*
```

Example:

```text
feature/customer-management
feature/inventory-module
release/v1.0.0
hotfix/authentication-fix
```

---

## Versioning

BSX Platform follows Semantic Versioning.

Example:

```text
v0.1.0
v0.2.0
v1.0.0
v2.0.0
```

---

## Product Roadmap

### Foundation

- [ ] Shared Kernel
- [ ] Building Blocks
- [ ] Identity Module
- [ ] Authorization
- [ ] Auditing

### Business Modules

- [ ] CRM
- [ ] Sales
- [ ] Inventory
- [ ] Purchasing
- [ ] Accounting

### Platform Features

- [ ] Notifications
- [ ] Background Jobs
- [ ] Analytics
- [ ] AI Assistant

### Future

- [ ] Multi-tenancy
- [ ] Mobile Application
- [ ] Public API
- [ ] SaaS Edition

---

## License

This project is currently developed as a personal long-term platform initiative by Javier Baena Santa-Cruz.

All rights reserved.


BSX Platform • Built for Growth.
