using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;
using BSX.Modules.Inventory.Application.Dtos;

namespace BSX.Modules.Inventory.Application.Products.GetProductById;

/// <summary>Gets a product projection by identifier within the active tenant.</summary>
/// <param name="ProductId">The product identifier.</param>
[RequirePermission(InventoryPermissions.ProductsRead)]
public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;
