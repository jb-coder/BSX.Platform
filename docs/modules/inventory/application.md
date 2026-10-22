# Inventory - Application Layer

Status: Application implemented. Infrastructure, HTTP API and UI are out of scope for this slice.

## Principles

- **CQRS** (ADR-002): commands modify state through aggregates; queries project straight to DTOs.
- **Result pattern** (ADR-011): every request returns `Result` / `Result<T>`; expected failures are
  values, not exceptions.
- **Business rules stay in the Domain.** Handlers resolve the tenant, enforce the cross-aggregate
  policy, invoke aggregate behavior, and persist through the unit of work.
- **No Infrastructure dependency.** EF Core, `DbContext` and transports are absent.

## Project

`src/Modules/Inventory/BSX.Modules.Inventory.Application` references `BSX.SharedKernel`,
`BSX.Contracts`, `BSX.BuildingBlocks` and the Inventory Domain only.

`AddInventoryApplication()` registers the handlers, validators, `InventoryTenantResolver`,
`InventoryStockPolicy` and `InventoryPermissionCatalog`.

## Multi-tenancy

- The tenant always comes from `ITenantContext` through `InventoryTenantResolver`.
- No command or query accepts a `TenantId`.
- Every aggregate read and read-model call is tenant-scoped.
- A missing or empty tenant fails closed with `INVENTORY.Tenant.Required` (403).

## Commands

| Area | Command | Permission | Result |
| --- | --- | --- | --- |
| Products | `CreateProduct` | `inventory.products.create` | New product id |
| | `UpdateProduct` | `inventory.products.update` | success |
| | `ActivateProduct`, `DeactivateProduct` | `inventory.products.update` | success |
| Warehouses | `CreateWarehouse` | `inventory.warehouses.create` | New warehouse id |
| | `UpdateWarehouse` | `inventory.warehouses.update` | success |
| | `ActivateWarehouse`, `DeactivateWarehouse` | `inventory.warehouses.update` | success |
| Stock | `ReceiveStock` | `inventory.stock.update` | Inventory item id |
| | `ReserveStock`, `ReleaseReservation`, `AdjustStock` | `inventory.stock.update` | success |

`UpdateProduct` renames the product; the SKU is immutable. `UpdateWarehouse` renames the warehouse.
Activation and deactivation propagate the Domain lifecycle conflicts unchanged.

## Queries

| Query | Permission | Result |
| --- | --- | --- |
| `GetProductById` | `inventory.products.read` | `ProductDto` |
| `ListProducts` | `inventory.products.read` | `PagedResult<ProductSummaryDto>` |
| `GetWarehouseById` | `inventory.warehouses.read` | `WarehouseDto` |
| `ListWarehouses` | `inventory.warehouses.read` | `PagedResult<WarehouseSummaryDto>` |
| `GetInventoryItemById` | `inventory.stock.read` | `InventoryItemDto` |
| `ListInventoryItems` | `inventory.stock.read` | `PagedResult<InventoryItemSummaryDto>` |

## Read model

- `IProductReadRepository`, `IWarehouseReadRepository` and `IInventoryReadRepository` are
  Application-owned ports implemented by Infrastructure.
- Detail and list queries project directly to DTOs; **list queries never load aggregates**.
- Paging is normalized in the handlers (page >= 1; size 1-100, default 20).
- Search semantics are passed through to Infrastructure (product SKU/name, warehouse name, or the
  joined product/warehouse text for stock positions).
- `InventoryItemDto` and `InventoryItemSummaryDto` expose `AvailableQuantity` as projected by the
  Infrastructure read model; it must equal `OnHandQuantity - ReservedQuantity`.

## Mandatory stock policy

`InventoryStockPolicy` centralizes the policy documented in
[application-policy.md](application-policy.md). Before any stock change it:

1. Resolves the tenant.
2. Loads the product inside the tenant (missing or cross-tenant is `NotFound`).
3. Rejects an inactive product.
4. Loads the warehouse inside the tenant (missing or cross-tenant is `NotFound`).
5. Rejects an inactive warehouse.
6. Resolves the stock position by `(TenantId, ProductId, WarehouseId)`.
7. For `ReceiveStock`, returns a new in-memory position when none exists, persisting it only after
   the receipt succeeds. Every other stock command requires an existing position.

The policy is enforced for `ReceiveStock`, `ReserveStock`, `ReleaseReservation` and `AdjustStock`.
It is never moved into the Domain.

## DTOs

`ProductDto`, `ProductSummaryDto`, `WarehouseDto`, `WarehouseSummaryDto`, `InventoryItemDto`,
`InventoryItemSummaryDto` and `PagedResult<T>`. DTOs contain primitives only; no Domain types cross
the Application boundary.

## Validation

FluentValidation validators live beside each command and validate shape only: required strings,
maximum lengths and non-empty identifiers. Quantity/pricing/lifecycle rules are **not** duplicated;
the Domain returns `INVENTORY.*` validation/conflict errors.

Queries normalize paging and validate identifiers in the handler, matching CRM and Sales.

## Errors

`InventoryApplicationErrors` owns the cross-aggregate codes:

- `INVENTORY.Tenant.Required`
- `INVENTORY.Product.NotFound`, `INVENTORY.Product.Inactive`,
  `INVENTORY.Product.InvalidId`, `INVENTORY.Product.SkuAlreadyExists`
- `INVENTORY.Warehouse.NotFound`, `INVENTORY.Warehouse.Inactive`,
  `INVENTORY.Warehouse.InvalidId`
- `INVENTORY.InventoryItem.NotFound`, `INVENTORY.InventoryItem.InvalidId`

Domain failures (`INVENTORY.Sku.Invalid`, `INVENTORY.Stock.*`, lifecycle conflicts) propagate from
`InventoryErrors` unchanged.

## Permissions

```text
inventory.products.read
inventory.products.create
inventory.products.update

inventory.warehouses.read
inventory.warehouses.create
inventory.warehouses.update

inventory.stock.read
inventory.stock.update
```

Every request declares exactly one `[RequirePermission]`; the Application tests lock this. The codes
are published by `InventoryPermissionCatalog` (`IPermissionCatalogSource`) so Identity can grant
them to the system roles (ADR-018).

## Testing

`tests/Modules/Inventory/BSX.Modules.Inventory.Application.UnitTests` covers:

- Every command and query handler.
- Tenant resolution and missing-tenant failure.
- The mandatory product/warehouse/stock-item policy for every stock command, including inactive and
  cross-tenant aggregates.
- Receive creating a position only after a successful receipt.
- Domain-failure propagation (invalid values, insufficient stock, adjustment conflicts).
- Repository interactions and tenant scoping.
- Permission attributes for every request and the permission catalog.
- Validator behavior, including not duplicating Domain quantity rules.

## Out of scope

Infrastructure, HTTP API, UI, purchasing, suppliers, locations, transfers, picking, shipping,
orders, invoices and reporting.
