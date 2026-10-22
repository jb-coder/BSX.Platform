using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Inventory.ReserveStock;

/// <summary>Reserves free stock for a product in a warehouse.</summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="WarehouseId">The warehouse identifier.</param>
/// <param name="Quantity">The positive quantity to reserve.</param>
[RequirePermission(InventoryPermissions.StockUpdate)]
public sealed record ReserveStockCommand(Guid ProductId, Guid WarehouseId, int Quantity) : ICommand;
