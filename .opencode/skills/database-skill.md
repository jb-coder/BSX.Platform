# BSX Platform Database Skill

## Purpose

This document defines database standards and persistence rules for BSX Platform.

The platform uses PostgreSQL and Entity Framework Core.

The goal is ensuring consistency, performance, maintainability and future scalability.

---

# Database Engine

Official database:

PostgreSQL

No other provider should influence architectural decisions.

The domain must remain database agnostic.

---

# ORM

Official ORM:

Entity Framework Core

Guidelines:

- Code First approach
- Migrations tracked in source control
- Explicit entity configurations
- Avoid data annotations when Fluent API is clearer

---

# Entity Configuration

Every aggregate must have its own configuration.

Preferred:

Infrastructure
└── Persistence
    └── Configurations
        └── CustomerConfiguration.cs

Avoid large configuration files.

---

# Primary Keys

Use strongly typed identifiers when possible.

Preferred:

CustomerId
OrderId
InvoiceId

Avoid primitive obsession.

---

# Auditing

Every aggregate should support auditing.

Standard fields:

- CreatedOnUtc
- CreatedBy
- UpdatedOnUtc
- UpdatedBy

Auditing must be automatic.

---

# Soft Delete

Use soft delete for business data.

Required fields:

- DeletedOnUtc
- DeletedBy
- IsDeleted

Hard deletes should be rare.

---

# Transactions

Commands modifying business data must execute inside transactions.

Queries must never start transactions.

---

# Repositories

Repositories belong to aggregates.

Allowed:

ICustomerRepository

IOrderRepository

Forbidden:

IRepository<T>

---

# Indexing

Create indexes intentionally.

Examples:

Email

CustomerNumber

OrderNumber

InvoiceNumber

Never assume EF Core will optimize queries automatically.

---

# Query Performance

Always project to DTOs.

Avoid loading complete aggregates for read-only screens.

Preferred:

Select projections.

Avoid:

Include chains for reporting screens.

---

# Migrations

One migration per feature batch.

Naming:

AddCustomerModule

AddInventoryTables

AddAccountingEntries

Migration names must be meaningful.

---

# Outbox Pattern

The architecture must be prepared for Outbox Pattern implementation.

Future message brokers:

- RabbitMQ
- Azure Service Bus
- Kafka

Database design should not make Outbox adoption difficult.

---

# Multi-Tenancy Readiness

Design entities to support future tenant isolation.

Do not implement yet.

Database evolution must remain possible without major refactoring.