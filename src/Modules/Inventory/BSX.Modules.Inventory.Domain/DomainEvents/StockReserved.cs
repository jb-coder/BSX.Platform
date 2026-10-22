using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Inventory.Domain.DomainEvents;

/// <summary>Raised when stock is reserved in an inventory item.</summary>
/// <param name="InventoryItemId">The inventory item identifier.</param>
/// <param name="Quantity">The reserved quantity.</param>
public sealed record StockReserved(InventoryItemId InventoryItemId, Quantity Quantity) : DomainEvent;
