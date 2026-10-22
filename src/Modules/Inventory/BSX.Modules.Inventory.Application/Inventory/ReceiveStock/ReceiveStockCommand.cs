using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Inventory.ReceiveStock;

/// <summary>
/// Receives stock into the product/warehouse position, creating the empty position when it does not
/// exist yet. Returns the inventory item identifier.
/// </summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="WarehouseId">The warehouse identifier.</param>
/// <param name="Quantity">The positive quantity received.</param>
[RequirePermission(InventoryPermissions.StockUpdate)]
public sealed record ReceiveStockCommand(Guid ProductId, Guid WarehouseId, int Quantity) : ICommand<Guid>;
