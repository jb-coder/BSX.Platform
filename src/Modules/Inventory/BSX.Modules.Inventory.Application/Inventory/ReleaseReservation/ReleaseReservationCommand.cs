using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Inventory.ReleaseReservation;

/// <summary>Releases previously reserved stock.</summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="WarehouseId">The warehouse identifier.</param>
/// <param name="Quantity">The positive quantity to release.</param>
[RequirePermission(InventoryPermissions.StockUpdate)]
public sealed record ReleaseReservationCommand(Guid ProductId, Guid WarehouseId, int Quantity) : ICommand;
