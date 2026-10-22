# Inventory - Domain Model

Status: **Domain and Application implemented**. Infrastructure, HTTP API and UI are out of scope
for this slice. See [application.md](application.md).

## Scope (Inventory v1)

The Inventory domain models three aggregates:

- `Product` - a sellable or stockable item identified by a tenant-unique SKU.
- `Warehouse` - a physical or logical stock location.
- `InventoryItem` - the stock position of one product in one warehouse.

Purchasing, suppliers, transfers, picking, shipping, orders, invoices and reporting are explicitly
out of scope.

The domain is persistence-independent: it references `BSX.SharedKernel` only (no EF Core,
BuildingBlocks, Persistence or another module).

---

## Stock Semantics (authoritative)

Inventory v1 uses three quantities on `InventoryItem`. They are intentionally distinct:

| Quantity | Business meaning | Formula |
| --- | --- | --- |
| `OnHandQuantity` | The total physical stock in the warehouse. This is the authoritative physical count and **includes** reserved stock. | Absolute quantity |
| `ReservedQuantity` | The portion of on-hand stock committed to reservations, but still physically present and not yet issued/adjusted out. | Absolute quantity |
| `AvailableQuantity` | The **free** stock that can still be reserved by new commitments. | `OnHandQuantity - ReservedQuantity` |

Enforced invariants:

```text
0 <= ReservedQuantity <= OnHandQuantity
AvailableQuantity = OnHandQuantity - ReservedQuantity >= 0
```

Clarification of the original requirement "reserved quantity never exceeds available quantity":
in this model **"available" in that sentence means on-hand stock**, not the derived free quantity.
`ReservedQuantity` may legitimately be greater than `AvailableQuantity` (for example, on-hand 100,
reserved 80, free/available 20). What must never happen is `ReservedQuantity > OnHandQuantity`,
because that would commit more stock than physically exists.

This definition is locked by tests in
`tests/Modules/Inventory/BSX.Modules.Inventory.Domain.UnitTests/InventoryItems/`.

---

## Application Orchestration

`InventoryItem` references `Product` and `Warehouse` by identifier only. It cannot verify
existence, lifecycle or tenant. Before any stock-changing operation, the Application layer must
enforce the mandatory product/warehouse policy in
[application-policy.md](application-policy.md).

---

## Aggregates

### Product (`AggregateRoot<ProductId>`)

| Member | Meaning |
| --- | --- |
| `TenantId` | Tenant identifier (ADR-013). |
| `Sku` | Tenant-unique stock-keeping unit code. |
| `Name` | Product name. |
| `Status` | `Active` or `Inactive`. |

Behavior: `Create`, `Rename`, `Activate`, `Deactivate`.

- `Create` raises `ProductCreated` and starts `Active`.
- `Rename` is allowed in any status and raises no event.
- `Activate` raises `ProductActivated`; activating an active product is a conflict.
- `Deactivate` raises `ProductDeactivated`; deactivating an inactive product is a conflict.
- SKU uniqueness is **delegated to Infrastructure** (`IProductRepository.ExistsBySkuAsync`); the
  domain only validates and normalizes the value.

### Warehouse (`AggregateRoot<WarehouseId>`)

| Member | Meaning |
| --- | --- |
| `TenantId` | Tenant identifier (ADR-013). |
| `Name` | Warehouse name. |
| `Status` | `Active` (operational) or `Inactive`. |

Behavior: `Create`, `Rename`, `Activate`, `Deactivate`.

- `Create` raises `WarehouseCreated` and starts operational (`Active`).
- `Rename` is allowed in any status and raises no event.
- `Activate` raises `WarehouseActivated`; activating an active warehouse is a conflict.
- `Deactivate` raises `WarehouseDeactivated`; deactivating an inactive warehouse is a conflict.

### InventoryItem (`AggregateRoot<InventoryItemId>`)

The consistency boundary for stock quantities.

| Member | Meaning |
| --- | --- |
| `TenantId` | Tenant identifier (ADR-013). |
| `ProductId` | Product reference (identifier only). |
| `WarehouseId` | Warehouse reference (identifier only). |
| `OnHandQuantity` | Total quantity physically on hand. |
| `ReservedQuantity` | Quantity reserved for future consumption. |
| `AvailableQuantity` | Derived: `OnHandQuantity - ReservedQuantity`. |

Behavior: `Create`, `Receive`, `Reserve`, `Release`, `Adjust`.

Stock rules:

| Operation | Rule | Event |
| --- | --- | --- |
| `Receive(quantity)` | Quantity must be positive; on-hand must stay within range. | `StockReceived` |
| `Reserve(quantity)` | Quantity must be positive and `<= AvailableQuantity`. | `StockReserved` |
| `Release(quantity)` | Quantity must be positive and `<= ReservedQuantity`. | `StockReleased` |
| `Adjust(delta)` | `delta != 0`; resulting on-hand `>= 0`, `<= MaxValue` and `>= ReservedQuantity`. | `StockAdjusted` |

Invariants:

- `OnHandQuantity >= 0`.
- `ReservedQuantity >= 0`.
- `ReservedQuantity <= OnHandQuantity`.
- `AvailableQuantity` is never negative.
- A failed operation leaves the aggregate unchanged and raises no event.
- One stock position exists per `(TenantId, ProductId, WarehouseId)`; uniqueness is delegated to
  Infrastructure (`IInventoryItemRepository.ExistsByProductAndWarehouseAsync`).
- Uniqueness checks are advisory: Infrastructure must enforce tenant-scoped unique indexes
  (partial where soft delete applies). The database constraint is authoritative; concurrent
  requests can race the check-then-write.

---

## Value Objects

| Value object | Rules |
| --- | --- |
| `ProductId`, `WarehouseId`, `InventoryItemId`, `TenantId` | Strongly typed, non-empty GUIDs; equality includes the type. |
| `Sku` | Trimmed, upper-case invariant; 1-64 characters; letters, digits, `-`, `_`, `.`. |
| `ProductName` | Trimmed; 1-200 characters. |
| `WarehouseName` | Trimmed; 1-200 characters. |
| `Quantity` | Whole number from `0` to `1,000,000,000`; zero allowed. Discrete units only (ADR-019). |

---

## Domain Events

- `ProductCreated`, `ProductActivated`, `ProductDeactivated`
- `WarehouseCreated`, `WarehouseActivated`, `WarehouseDeactivated`
- `StockReceived`, `StockReserved`, `StockReleased`, `StockAdjusted`

All events derive from `DomainEvent` and are internal to the module. They are persisted and
dispatched through the platform outbox (ADR-009); they never cross a module boundary.

---

## Repository Contracts

| Contract | Responsibilities |
| --- | --- |
| `IProductRepository` | `GetByIdAsync(TenantId, ProductId)`, `ExistsBySkuAsync(TenantId, Sku)`, `AddAsync(Product)`. |
| `IWarehouseRepository` | `GetByIdAsync(TenantId, WarehouseId)`, `AddAsync(Warehouse)`. |
| `IInventoryItemRepository` | `GetByIdAsync(TenantId, InventoryItemId)`, `GetByProductAndWarehouseAsync(TenantId, ProductId, WarehouseId)`, `ExistsByProductAndWarehouseAsync(...)`, `AddAsync(InventoryItem)`. |

Every read is tenant-scoped and repositories are aggregate-specific (no generic repository).

---

## Multi-Tenancy

- Every aggregate carries `TenantId`.
- Every repository read takes `TenantId` as its first parameter.
- SKU uniqueness and the product/warehouse stock-position uniqueness are scoped per tenant.
- Tenant isolation is ultimately enforced by Infrastructure query filters, matching CRM and Sales.

---

## Inventory v1 Constraints

These are deliberate limitations, not unimplemented features:

| Constraint | Meaning |
| --- | --- |
| **Discrete units only** | `Quantity` is an integer; no unit of measure, weight, volume or fractional stock ([ADR-019](../../decisions/ADR-019-Inventory-Quantity-Strategy.md)). |
| **No reservation identities** | Reservations are an aggregate quantity; there is no reservation id/record per commitment. |
| **No reservation ownership** | A reservation is not attributed to an order, customer, user or module. |
| **No reservation expiry** | Reserved stock never expires automatically; it must be released explicitly. |
| **No warehouse locations** | There are no bins, zones, aisles or location-level stock positions. |
| **Warehouse-level stock only** | `InventoryItem` is keyed by `(TenantId, ProductId, WarehouseId)`; stock is not tracked below the warehouse. |

Consequences:

- Future Sales/Purchasing integration needs a reservation ledger or an aggregate extension to
  release/expire a specific reservation.
- Warehouse automation or picking requires an explicit location dimension and a migration of the
  stock-position key.
- Weight/volume-based inventory requires a superseding quantity ADR and a data migration.

---

## Out of Scope

Infrastructure, HTTP API, UI, purchasing, suppliers, transfers, picking, shipping, orders,
invoices, reporting, reservations across warehouses and stock valuation.
