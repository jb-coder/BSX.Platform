using BSX.Modules.Inventory.Domain.DomainEvents;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Domain.InventoryItems;

/// <summary>
/// The stock of one product in one warehouse. This is the consistency boundary for quantities.
/// <para>
/// Stock semantics (Inventory v1):
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="OnHandQuantity"/> is the total physical stock in the warehouse, including stock that
/// is reserved.
/// </description></item>
/// <item><description>
/// <see cref="ReservedQuantity"/> is the portion of <see cref="OnHandQuantity"/> committed to
/// reservations but not yet removed from stock.
/// </description></item>
/// <item><description>
/// <see cref="AvailableQuantity"/> is the free quantity that can still be reserved:
/// <c>OnHandQuantity - ReservedQuantity</c>.
/// </description></item>
/// </list>
/// <para>
/// The enforced invariant is <c>ReservedQuantity &lt;= OnHandQuantity</c>. Therefore
/// <see cref="AvailableQuantity"/> is never negative. <see cref="ReservedQuantity"/> may be greater
/// than <see cref="AvailableQuantity"/> when most of the physical stock is already committed; the
/// original phrase "reserved never exceeds available" is interpreted as "reserved never exceeds
/// on-hand" (see <c>docs/modules/inventory/domain-model.md</c>).
/// </para>
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

    /// <summary>
    /// Gets the total physical quantity on hand, including reserved stock. This is the authoritative
    /// physical count for the product/warehouse pair.
    /// </summary>
    public Quantity OnHandQuantity { get; private set; } = null!;

    /// <summary>
    /// Gets the quantity committed to reservations. Reserved stock is still part of
    /// <see cref="OnHandQuantity"/> until it is issued or adjusted out; it only makes the stock
    /// unavailable for new reservations.
    /// </summary>
    public Quantity ReservedQuantity { get; private set; } = null!;

    /// <summary>
    /// Gets the free quantity available for new reservations:
    /// <c>OnHandQuantity - ReservedQuantity</c>. It is never negative because
    /// <see cref="ReservedQuantity"/> can never exceed <see cref="OnHandQuantity"/>.
    /// </summary>
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

    /// <summary>
    /// Reserves free stock. The quantity is added to <see cref="ReservedQuantity"/> and must not
    /// exceed <see cref="AvailableQuantity"/>.
    /// </summary>
    /// <param name="quantity">The reserved quantity; must be positive and not exceed free available stock.</param>
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

    /// <summary>
    /// Releases a reservation. The quantity is removed from <see cref="ReservedQuantity"/> and
    /// becomes available for new reservations again.
    /// </summary>
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
