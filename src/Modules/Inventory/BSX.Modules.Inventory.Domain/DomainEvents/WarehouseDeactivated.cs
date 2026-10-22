using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Inventory.Domain.DomainEvents;

/// <summary>Raised when a warehouse is deactivated.</summary>
/// <param name="WarehouseId">The warehouse identifier.</param>
public sealed record WarehouseDeactivated(WarehouseId WarehouseId) : DomainEvent;
