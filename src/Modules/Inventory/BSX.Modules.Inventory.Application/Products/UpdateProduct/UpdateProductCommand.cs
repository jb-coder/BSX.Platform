using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Products.UpdateProduct;

/// <summary>Renames a product. The SKU is immutable.</summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="Name">The new product name.</param>
[RequirePermission(InventoryPermissions.ProductsUpdate)]
public sealed record UpdateProductCommand(Guid ProductId, string Name) : ICommand;
