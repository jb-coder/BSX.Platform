# Inventory - Application Orchestration Policy

Status: **Implemented in `BSX.Modules.Inventory.Application`.** This document remains the
mandatory contract for every stock-changing use case.

The Inventory domain deliberately owns one aggregate at a time. `InventoryItem` references
`Product` and `Warehouse` by identifier only and cannot verify their existence, lifecycle status or
tenant. The Application layer must enforce those cross-aggregate rules before invoking any
stock-changing domain operation.

---

## Scope

This policy applies to every command that creates a stock position or changes stock:

- `ReceiveStock` (the v1 stock-position creation path)
- `ReserveStock`
- `ReleaseReservation`
- `AdjustStock`

---

## Mandatory preconditions

Before any stock-changing operation, the Application handler must:

1. **Resolve the tenant.**
   - Require an active tenant from `ITenantContext` / the authenticated scope.
   - If no tenant is available, fail closed (no data, `Authentication`/`Permission` failure).

2. **Load the product by tenant.**
   - `IProductRepository.GetByIdAsync(tenantId, productId)`.
   - A missing product, or a product from another tenant, is a `NotFound` failure. Do not reveal
     cross-tenant existence.

3. **Load the warehouse by tenant.**
   - `IWarehouseRepository.GetByIdAsync(tenantId, warehouseId)`.
   - A missing warehouse, or a warehouse from another tenant, is a `NotFound` failure.

4. **Require the product to be active.**
   - `product.Status == ProductStatus.Active`.
   - An inactive product is a conflict for stock-changing operations.

5. **Require the warehouse to be operational.**
   - `warehouse.Status == WarehouseStatus.Active`.
   - An inactive warehouse is a conflict for stock-changing operations.

6. **Require the same tenant.**
   - The tenant-scoped reads above are the enforcement mechanism.
   - Never trust `ProductId`/`WarehouseId` supplied by the caller without loading both aggregates
     inside the current tenant.

7. **Resolve the inventory item.**
   - For existing stock operations, load
     `IInventoryItemRepository.GetByProductAndWarehouseAsync(tenantId, productId, warehouseId)`.
   - A missing item is a `NotFound` failure for `ReserveStock`, `ReleaseReservation` and
     `AdjustStock`.
   - `ReceiveStock` is the explicit v1 creation path: when no position exists it creates an empty
     `InventoryItem` in memory, applies the receipt, and only then adds it to the repository. A
     failed receipt (for example a zero quantity) therefore leaves no position behind.
   - `(TenantId, ProductId, WarehouseId)` uniqueness is enforced by the Infrastructure unique
     index; the Application pre-check remains advisory and a concurrent insert is a conflict.

---

## Operation policy

| Operation | Product exists | Warehouse exists | Product active | Warehouse active | Same tenant | Stock item |
| --- | --- | --- | --- | --- | --- | --- |
| Receive | Required | Required | Required | Required | Required | Created if missing |
| Reserve | Required | Required | Required | Required | Required | Required |
| Release | Required | Required | Required | Required | Required | Required |
| Adjust | Required | Required | Required | Required | Required | Required |

Release and Adjust are stock-changing operations and therefore follow the same active
product/warehouse policy. If future operations must be allowed after deactivation (for example,
administrative write-off), that requires an explicit policy exception.

---

## Deactivation policy (v1 decision point)

The domain only enforces the Active/Inactive transition; it does not know whether stock or
reservations exist. The Application layer must decide:

- whether deactivating a product is blocked while any stock position exists,
- whether deactivating a warehouse is blocked while stock or reservations exist,
- how an operator resolves the conflict (issue, adjust to zero, transfer, or force deactivate).

Until that policy is approved, deactivation remains a domain lifecycle operation and must not be
used as a substitute for stock disposal.

---

## Transaction and concurrency

- Load the aggregates, apply the domain operation and persist through the module unit of work in a
  single command transaction.
- A multi-line operation loads multiple `InventoryItem` aggregates; each aggregate keeps its own
  optimistic concurrency token.
- Uniqueness checks (`ExistsBySkuAsync`, `ExistsByProductAndWarehouseAsync`) are advisory.
  Infrastructure must enforce tenant-scoped unique indexes (partial where soft delete applies);
  the Application maps unique violations to conflict results.

---

## Uniqueness and race conditions

- SKU uniqueness and product/warehouse stock-position uniqueness are **Infrastructure-enforced**
  by unique indexes, not by the domain.
- Check-then-write is only a friendly pre-check; concurrent requests can race.
- The database constraint is the final authority and must be mapped to a stable conflict error.

---

## Error ownership

- Domain errors remain in `InventoryErrors` (`INVENTORY.*`).
- Missing/inactive product or warehouse errors are Application concerns; define them in the
  Inventory Application error catalog when the Application layer is implemented.
- Do not add cross-aggregate product/warehouse checks to `InventoryItem`.

---

## Non-goals

This document does not define HTTP mapping or transport concerns. Command/query shapes, DTOs,
validators and error codes are documented in [application.md](application.md).
