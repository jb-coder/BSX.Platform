using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Warehouses.UpdateWarehouse;

/// <summary>Renames a warehouse.</summary>
/// <param name="WarehouseId">The warehouse identifier.</param>
/// <param name="Name">The new warehouse name.</param>
[RequirePermission(InventoryPermissions.WarehousesUpdate)]
public sealed record UpdateWarehouseCommand(Guid WarehouseId, string Name) : ICommand;
