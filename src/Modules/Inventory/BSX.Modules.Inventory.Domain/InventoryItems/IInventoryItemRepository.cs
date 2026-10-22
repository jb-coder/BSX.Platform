using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.InventoryItems;

/// <summary>Persistence contract for the <see cref="InventoryItem"/> aggregate (aggregate-specific).</summary>
public interface IInventoryItemRepository
{
    /// <summary>Gets an inventory item by identifier within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier (read isolation).</param>
    /// <param name="id">The inventory item identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<InventoryItem?> GetByIdAsync(
        TenantId tenantId,
        InventoryItemId id,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the stock position for a product in a warehouse within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="warehouseId">The warehouse identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<InventoryItem?> GetByProductAndWarehouseAsync(
        TenantId tenantId,
        ProductId productId,
        WarehouseId warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a stock position already exists for the product/warehouse pair within a
    /// tenant. Uniqueness is an Infrastructure concern.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="warehouseId">The warehouse identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> ExistsByProductAndWarehouseAsync(
        TenantId tenantId,
        ProductId productId,
        WarehouseId warehouseId,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a new inventory item.</summary>
    /// <param name="item">The inventory item.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);
}
