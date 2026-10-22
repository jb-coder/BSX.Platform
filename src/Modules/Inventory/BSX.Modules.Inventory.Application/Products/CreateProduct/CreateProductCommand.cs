using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Inventory.Application.Common;

namespace BSX.Modules.Inventory.Application.Products.CreateProduct;

/// <summary>Creates a product in the active tenant.</summary>
/// <param name="Sku">The tenant-unique stock-keeping unit code.</param>
/// <param name="Name">The product name.</param>
[RequirePermission(InventoryPermissions.ProductsCreate)]
public sealed record CreateProductCommand(string Sku, string Name) : ICommand<Guid>;
