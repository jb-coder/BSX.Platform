using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Inventory.Domain.DomainEvents;

/// <summary>Raised when the on-hand stock of an inventory item is adjusted.</summary>
/// <param name="InventoryItemId">The inventory item identifier.</param>
/// <param name="Delta">The signed adjustment applied to the on-hand quantity.</param>
/// <param name="OnHandQuantity">The resulting on-hand quantity.</param>
public sealed record StockAdjusted(
    InventoryItemId InventoryItemId,
    int Delta,
    Quantity OnHandQuantity) : DomainEvent;
