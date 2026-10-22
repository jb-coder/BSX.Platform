using BSX.Modules.Inventory.Domain.DomainEvents;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Domain.InventoryItems;

/// <summary>
/// The stock of one product in one warehouse. This is the consistency boundary for quantities:
/// reserved stock can never exceed on-hand stock and on-hand stock can never be negative.
/// The product and warehouse are referenced by identifier only (ADR-003, ADR-013).
/// </summary>
public sealed class InventoryItem : AggregateRoot<InventoryItemId>
{
    private InventoryItem(
        InventoryItemId id,
        TenantId tenantId,
        ProductId productId,
        WarehouseId warehouseId)
        : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        WarehouseId = warehouseId;
        OnHandQuantity = Quantity.Zero;
        ReservedQuantity = Quantity.Zero;
    }

    private InventoryItem()
    {
    }

    /// <summary>Gets the tenant identifier.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the product identifier.</summary>
    public ProductId ProductId { get; private set; } = null!;

    /// <summary>Gets the warehouse identifier.</summary>
    public WarehouseId WarehouseId { get; private set; } = null!;

    /// <summary>Gets the total quantity physically on hand.</summary>
    public Quantity OnHandQuantity { get; private set; } = null!;

    /// <summary>Gets the quantity currently reserved.</summary>
    public Quantity ReservedQuantity { get; private set; } = null!;

    /// <summary>Gets the quantity that can still be reserved.</summary>
    public Quantity AvailableQuantity => Quantity.From(OnHandQuantity.Value - ReservedQuantity.Value);

    /// <summary>Creates an empty stock position for a product in a warehouse.</summary>
    /// <param name="id">The inventory item identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="warehouseId">The warehouse identifier.</param>
    public static Result<InventoryItem> Create(
        InventoryItemId id,
        TenantId tenantId,
        ProductId productId,
        WarehouseId warehouseId)
        => Result.Success(new InventoryItem(id, tenantId, productId, warehouseId));

    /// <summary>Receives stock, increasing the on-hand quantity.</summary>
    /// <param name="quantity">The received quantity; must be positive.</param>
    public Result Receive(Quantity quantity)
    {
        if (quantity.IsZero)
        {
            return Result.Failure(InventoryErrors.StockQuantityNotPositive);
        }

        long newOnHand = (long)OnHandQuantity.Value + quantity.Value;

        if (newOnHand > Quantity.MaxValue)
        {
            return Result.Failure(InventoryErrors.StockQuantityOverflow);
        }

        OnHandQuantity = Quantity.From((int)newOnHand);
        RaiseDomainEvent(new StockReceived(Id, quantity));
        return Result.Success();
    }

    /// <summary>Reserves stock, moving quantity from available to reserved.</summary>
    /// <param name="quantity">The reserved quantity; must be positive and not exceed available stock.</param>
    public Result Reserve(Quantity quantity)
    {
        if (quantity.IsZero)
        {
            return Result.Failure(InventoryErrors.StockQuantityNotPositive);
        }

        if (quantity.Value > AvailableQuantity.Value)
        {
            return Result.Failure(InventoryErrors.InsufficientAvailableStock);
        }

        ReservedQuantity = Quantity.From(ReservedQuantity.Value + quantity.Value);
        RaiseDomainEvent(new StockReserved(Id, quantity));
        return Result.Success();
    }

    /// <summary>Releases a reservation, moving quantity from reserved back to available.</summary>
    /// <param name="quantity">The released quantity; must be positive and not exceed reserved stock.</param>
    public Result Release(Quantity quantity)
    {
        if (quantity.IsZero)
        {
            return Result.Failure(InventoryErrors.StockQuantityNotPositive);
        }

        if (quantity.Value > ReservedQuantity.Value)
        {
            return Result.Failure(InventoryErrors.InsufficientReservedStock);
        }

        ReservedQuantity = Quantity.From(ReservedQuantity.Value - quantity.Value);
        RaiseDomainEvent(new StockReleased(Id, quantity));
        return Result.Success();
    }

    /// <summary>
    /// Adjusts the on-hand quantity by a signed delta. The resulting on-hand quantity must remain
    /// non-negative and cannot fall below the reserved quantity.
    /// </summary>
    /// <param name="delta">The signed adjustment to apply.</param>
    public Result Adjust(int delta)
    {
        if (delta == 0)
        {
            return Result.Failure(InventoryErrors.StockAdjustmentIsZero);
        }

        long newOnHand = (long)OnHandQuantity.Value + delta;

        if (newOnHand < Quantity.MinValue)
        {
            return Result.Failure(InventoryErrors.StockAdjustmentWouldBeNegative);
        }

        if (newOnHand > Quantity.MaxValue)
        {
            return Result.Failure(InventoryErrors.StockQuantityOverflow);
        }

        if (newOnHand < ReservedQuantity.Value)
        {
            return Result.Failure(InventoryErrors.StockAdjustmentBelowReserved);
        }

        OnHandQuantity = Quantity.From((int)newOnHand);
        RaiseDomainEvent(new StockAdjusted(Id, delta, OnHandQuantity));
        return Result.Success();
    }
}
