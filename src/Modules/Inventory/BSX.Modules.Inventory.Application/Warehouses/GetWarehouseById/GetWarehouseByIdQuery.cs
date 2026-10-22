using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;
using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.Warehouses.GetWarehouseById;

/// <summary>Gets a warehouse projection by identifier within the active tenant.</summary>
/// <param name="WarehouseId">The warehouse identifier.</param>
[RequirePermission(InventoryPermissions.WarehousesRead)]
public sealed record GetWarehouseByIdQuery(Guid WarehouseId) : IQuery<WarehouseDto>;
