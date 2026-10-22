using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Warehouses.DeactivateWarehouse;

/// <summary>Deactivates an active warehouse.</summary>
/// <param name="WarehouseId">The warehouse identifier.</param>
[RequirePermission(InventoryPermissions.WarehousesUpdate)]
public sealed record DeactivateWarehouseCommand(Guid WarehouseId) : ICommand;
