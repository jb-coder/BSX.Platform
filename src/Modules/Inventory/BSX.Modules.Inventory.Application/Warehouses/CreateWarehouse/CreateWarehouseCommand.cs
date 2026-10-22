using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Warehouses.CreateWarehouse;

/// <summary>Creates a warehouse in the active tenant.</summary>
/// <param name="Name">The warehouse name.</param>
[RequirePermission(InventoryPermissions.WarehousesCreate)]
public sealed record CreateWarehouseCommand(string Name) : ICommand<Guid>;
