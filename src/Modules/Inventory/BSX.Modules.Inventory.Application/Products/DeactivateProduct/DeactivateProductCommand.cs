using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Products.DeactivateProduct;

/// <summary>Deactivates an active product.</summary>
/// <param name="ProductId">The product identifier.</param>
[RequirePermission(InventoryPermissions.ProductsUpdate)]
public sealed record DeactivateProductCommand(Guid ProductId) : ICommand;
