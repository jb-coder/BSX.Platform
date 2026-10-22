using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Domain.InventoryItems;
using BSX.Modules.Inventory.Domain.Products;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.Modules.Inventory.Domain.Warehouses;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Application.Common;

/// <summary>
/// Enforces the mandatory Inventory application policy before any stock-changing operation
/// (see <c>docs/modules/inventory/application-policy.md</c>): the product and warehouse must exist
/// in the active tenant and be active, and the stock position must be resolved.
/// </summary>
public sealed class InventoryStockPolicy
{
    private readonly IProductRepository _products;
    private readonly IWarehouseRepository _warehouses;
    private readonly IInventoryItemRepository _inventoryItems;

    /// <summary>Initializes a new instance of the <see cref="InventoryStockPolicy"/> class.</summary>
    public InventoryStockPolicy(
        IProductRepository products,
        IWarehouseRepository warehouses,
        IInventoryItemRepository inventoryItems)
    {
        _products = products;
        _warehouses = warehouses;
        _inventoryItems = inventoryItems;
    }

    /// <summary>
    /// Validates the product/warehouse preconditions and resolves the stock position for the pair.
    /// When <paramref name="createIfMissing"/> is <see langword="true"/> and no position exists, a
    /// new empty position is returned but not persisted; the caller adds it only after the domain
    /// operation succeeds (Receive is the v1 stock-position creation path).
    /// </summary>
    /// <param name="tenantId">The active tenant identifier.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="warehouseId">The warehouse identifier.</param>
    /// <param name="createIfMissing">Whether a missing stock position may be created.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    public async Task<Result<InventoryStockResolution>> ResolveAsync(
        TenantId tenantId,
        ProductId productId,
        WarehouseId warehouseId,
        bool createIfMissing,
        CancellationToken cancellationToken = default)
    {
        Product? product = await _products
            .GetByIdAsync(tenantId, productId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
        {
            return Result.Failure<InventoryStockResolution>(InventoryApplicationErrors.ProductNotFound);
        }

        if (!product.IsActive)
        {
            return Result.Failure<InventoryStockResolution>(InventoryApplicationErrors.ProductInactive);
        }

        Warehouse? warehouse = await _warehouses
            .GetByIdAsync(tenantId, warehouseId, cancellationToken)
            .ConfigureAwait(false);

        if (warehouse is null)
        {
            return Result.Failure<InventoryStockResolution>(InventoryApplicationErrors.WarehouseNotFound);
        }

        if (!warehouse.IsOperational)
        {
            return Result.Failure<InventoryStockResolution>(InventoryApplicationErrors.WarehouseInactive);
        }

        InventoryItem? item = await _inventoryItems
            .GetByProductAndWarehouseAsync(tenantId, productId, warehouseId, cancellationToken)
            .ConfigureAwait(false);

        if (item is not null)
        {
            return Result.Success(new InventoryStockResolution(item, IsNew: false));
        }

        if (!createIfMissing)
        {
            return Result.Failure<InventoryStockResolution>(InventoryApplicationErrors.InventoryItemNotFound);
        }

        Result<InventoryItem> created = InventoryItem.Create(
            InventoryItemId.New(),
            tenantId,
            productId,
            warehouseId);

        return created.IsFailure
            ? Result.Failure<InventoryStockResolution>(created.Error)
            : Result.Success(new InventoryStockResolution(created.Value, IsNew: true));
    }
}

/// <summary>The stock position resolved by <see cref="InventoryStockPolicy"/>.</summary>
/// <param name="Item">The existing or newly created (not yet persisted) stock position.</param>
/// <param name="IsNew">Whether the item was created by this resolution and must be added.</param>
public sealed record InventoryStockResolution(InventoryItem Item, bool IsNew);
