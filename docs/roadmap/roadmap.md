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

Status: Domain, Application, Infrastructure (EF Core + PostgreSQL, outbox, auditing, soft delete), authentication, JWT, sessions, authorization, password reset, tenant isolation and HTTP API implemented and hardened, with a Testcontainers PostgreSQL parity suite. Identity is production-ready (single instance); web sign-in, workspace selection, shell and administration screens are implemented.


---

## Phase 3

CRM

- Customers
- Contacts
- Categories

Status: Domain, Application, Infrastructure and HTTP API implemented, tested and release-hardened (CQRS, tenant isolation, auditing, soft delete, optimistic concurrency mapped to `409`, projections, health, PostgreSQL parity suite). The full CRM web experience is implemented: customers, organizations, organization assignment,
contacts and addresses. Reporting and analytics are pending.

---

## Phase 4

Inventory

- [x] Products (domain)
- [x] Warehouses (domain)
- [x] Stock / inventory items (domain)
- [x] Products and warehouses (application commands, queries and permissions)
- [x] Stock commands, queries and the mandatory application policy

Status: Domain and Application implemented (`Product`, `Warehouse`, `InventoryItem` with lifecycle,
stock reservation rules, tenant-owned aggregates, domain events, product/warehouse commands and
queries, stock commands/queries and the mandatory stock policy). The Principal Architect review
conditions are resolved: stock semantics are formalized, Inventory v1 is discrete-units only
(ADR-019) and v1 limitations are recorded. Infrastructure, API and UI are not implemented. See
[domain-model.md](../modules/inventory/domain-model.md),
[application.md](../modules/inventory/application.md) and
[application-policy.md](../modules/inventory/application-policy.md).

---

## Platform Hardening

Status: **v0.4.0 — Done**. Identity, CRM and Sales are released. The foundation was hardened
without adding business modules:

- [x] ADR-009 transactional outbox (Identity, CRM, Sales): atomic persistence, exactly-once claim,
      retry/backoff, dead-letter, crash recovery.
- [x] ADR-018 platform permission catalog and declarative system-role reconciliation (add + remove).
- [x] Architecture governance (contracts, endpoints, domain isolation, dependency direction).
- [x] Observability (domain-event, outbox and permission-sync metrics/traces).
- [x] Distributed rate limiting abstractions (single-instance default; no Redis implementation yet).

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
permission enforcement, health checks, endpoint integration tests) implemented. The Sales web
experience (leads, lifecycle, opportunities, stage timeline and CRM pickers) is implemented;
quotations, orders and invoices remain out of scope for this slice.

Demo readiness is complete for CRM, Sales and Administration: opt-in sample data, guided home,
first-run progress, contextual empty states and a reusable customer/pilot walkthrough in
[demo-readiness.md](../demo/demo-readiness.md). Reporting remains out of scope.

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
