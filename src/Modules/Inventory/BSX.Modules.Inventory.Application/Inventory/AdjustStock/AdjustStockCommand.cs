using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Inventory.AdjustStock;

/// <summary>Adjusts the on-hand stock by a signed delta.</summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="WarehouseId">The warehouse identifier.</param>
/// <param name="Delta">The signed adjustment to apply.</param>
[RequirePermission(InventoryPermissions.StockUpdate)]
public sealed record AdjustStockCommand(Guid ProductId, Guid WarehouseId, int Delta) : ICommand;
