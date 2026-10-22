using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;
using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.Inventory.GetInventoryItemById;

/// <summary>Gets a stock-position projection by identifier within the active tenant.</summary>
/// <param name="InventoryItemId">The inventory item identifier.</param>
[RequirePermission(InventoryPermissions.StockRead)]
public sealed record GetInventoryItemByIdQuery(Guid InventoryItemId) : IQuery<InventoryItemDto>;
