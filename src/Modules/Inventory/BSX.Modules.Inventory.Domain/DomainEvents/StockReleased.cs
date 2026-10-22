using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Inventory.Domain.DomainEvents;

/// <summary>Raised when a reservation is released in an inventory item.</summary>
/// <param name="InventoryItemId">The inventory item identifier.</param>
/// <param name="Quantity">The released quantity.</param>
public sealed record StockReleased(InventoryItemId InventoryItemId, Quantity Quantity) : DomainEvent;
