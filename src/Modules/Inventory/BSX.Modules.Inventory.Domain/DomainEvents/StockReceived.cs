using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Inventory.Domain.DomainEvents;

/// <summary>Raised when stock is received into an inventory item.</summary>
/// <param name="InventoryItemId">The inventory item identifier.</param>
/// <param name="Quantity">The received quantity.</param>
public sealed record StockReceived(InventoryItemId InventoryItemId, Quantity Quantity) : DomainEvent;
