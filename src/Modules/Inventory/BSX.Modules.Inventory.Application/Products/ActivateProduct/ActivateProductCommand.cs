using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Products.ActivateProduct;

/// <summary>Activates an inactive product.</summary>
/// <param name="ProductId">The product identifier.</param>
[RequirePermission(InventoryPermissions.ProductsUpdate)]
public sealed record ActivateProductCommand(Guid ProductId) : ICommand;
