# BSX Platform Module Catalog

## Platform Modules

### Identity

Authentication and authorization.

### Notifications

Email and notifications.

### Background Jobs

Asynchronous execution.

---

## Business Modules

### CRM

Customer management.

### Sales

Leads and opportunities (domain, application, infrastructure and HTTP API implemented); quotations,
orders and invoices are planned.

### Inventory

Products, warehouses and stock. Domain and Application implemented (`Product`, `Warehouse`,
`InventoryItem` with product/warehouse lifecycle and stock commands/queries); Inventory v1 supports
discrete units only (ADR-019) and warehouse-level stock. Infrastructure, API and UI are planned.
See [domain-model.md](../modules/inventory/domain-model.md),
[application.md](../modules/inventory/application.md) and the mandatory
[application policy](../modules/inventory/application-policy.md).

### Purchasing

Suppliers and purchasing processes.

### Accounting

Financial records.

---

## Future Modules

### Human Resources

Employee management.

### Projects

Project management.

### Manufacturing

Production workflows.

### Analytics

Business intelligence.

### AI

Business assistant and automation.