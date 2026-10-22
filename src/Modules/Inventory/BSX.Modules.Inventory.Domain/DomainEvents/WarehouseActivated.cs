using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Inventory.Domain.DomainEvents;

/// <summary>Raised when a warehouse is activated.</summary>
/// <param name="WarehouseId">The warehouse identifier.</param>
public sealed record WarehouseActivated(WarehouseId WarehouseId) : DomainEvent;
