using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Inventory.Domain.DomainEvents;

/// <summary>Raised when a warehouse is created.</summary>
/// <param name="WarehouseId">The warehouse identifier.</param>
/// <param name="Name">The warehouse name.</param>
public sealed record WarehouseCreated(WarehouseId WarehouseId, WarehouseName Name) : DomainEvent;
