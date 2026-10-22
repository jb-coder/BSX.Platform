# BSX Platform Roadmap

## Phase 0

Foundation — Done

- [x] Documentation
- [x] ADRs
- [x] Architecture
- [x] Agents
- [x] Skills

---

## Phase 1

Core — Done

- [x] SharedKernel
- [x] BuildingBlocks
- [x] Result Pattern
- [x] Domain Events
- [x] In-process CQRS dispatcher
- [x] Validation and logging behaviors
- [x] Architecture tests
- [x] Build governance (.NET 10, central package management)
- [x] Architecture hardening (ADR-009 domain dispatch, ADR-010 auditing, ADR-011 errors, ADR-012 authorization)
- [x] Identity architecture hardening (ADR-013 multi-tenant identity, ADR-014 credentials, ADR-015 contracts, ADR-016 password reset, ADR-017 tokens/sessions, ADR-018 roles/permissions)
- [x] Foundation transaction and dispatch seam (ADR-009: TransactionBehavior + SaveChangesAndDispatchAsync + UnitOfWorkBase)

---

## Phase 2

Identity

- Users
- Roles
- Permissions
- JWT
- Refresh Tokens

Status: Domain, Application, Infrastructure (EF Core + PostgreSQL, outbox, auditing, soft delete), authentication, JWT, sessions, authorization, password reset, tenant isolation and HTTP API implemented and hardened. Identity is production-ready (single instance); UI pending.


---

## Phase 3

CRM

- Customers
- Contacts
- Categories

Status: Domain, Application, Infrastructure and HTTP API implemented, tested and release-hardened (CQRS, tenant isolation, auditing, soft delete, optimistic concurrency mapped to `409`, projections, health, PostgreSQL parity suite). Blazor UI, reporting and analytics pending.

---

## Phase 4

Inventory

- Products
- Warehouses
- Stock Movements

---

## Phase 5

Sales

- [x] Leads and Opportunities (domain + application + infrastructure + API)
- [ ] Quotations
- [ ] Orders
- [ ] Invoices

Status: Sales domain (Lead and Opportunity aggregates, value objects, events and repositories),
Application layer (commands, queries, handlers, validators, DTOs and tests), Infrastructure
(PostgreSQL persistence, repositories, tenant isolation, auditing, soft delete, concurrency,
migrations, contract-backed CRM reference validation and tests) and HTTP API (endpoints,
permission enforcement, health checks, endpoint integration tests) implemented. UI is pending;
quotations, orders and invoices remain out of scope for this slice.

---

## Phase 6

Accounting

- Journal Entries
- Fiscal Periods
- Ledger

---

## Phase 7

AI

- Business Assistant
- Analytics
- Recommendations
