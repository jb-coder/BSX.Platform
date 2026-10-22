using System.Reflection;
using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Inventory.Application.Common;
using BSX.Modules.Inventory.Application.Inventory.AdjustStock;
using BSX.Modules.Inventory.Application.Inventory.GetInventoryItemById;
using BSX.Modules.Inventory.Application.Inventory.ListInventoryItems;
using BSX.Modules.Inventory.Application.Inventory.ReceiveStock;
using BSX.Modules.Inventory.Application.Inventory.ReleaseReservation;
using BSX.Modules.Inventory.Application.Inventory.ReserveStock;
using BSX.Modules.Inventory.Application.Products.ActivateProduct;
using BSX.Modules.Inventory.Application.Products.CreateProduct;
using BSX.Modules.Inventory.Application.Products.DeactivateProduct;
using BSX.Modules.Inventory.Application.Products.GetProductById;
using BSX.Modules.Inventory.Application.Products.ListProducts;
using BSX.Modules.Inventory.Application.Products.UpdateProduct;
using BSX.Modules.Inventory.Application.Warehouses.ActivateWarehouse;
using BSX.Modules.Inventory.Application.Warehouses.CreateWarehouse;
using BSX.Modules.Inventory.Application.Warehouses.DeactivateWarehouse;
using BSX.Modules.Inventory.Application.Warehouses.GetWarehouseById;
using BSX.Modules.Inventory.Application.Warehouses.ListWarehouses;
using BSX.Modules.Inventory.Application.Warehouses.UpdateWarehouse;

namespace BSX.Modules.Inventory.Application.UnitTests.Permissions;

public sealed class InventoryPermissionTests
{
    private static readonly IReadOnlyDictionary<Type, string> Expected = new Dictionary<Type, string>
    {
        [typeof(CreateProductCommand)] = InventoryPermissions.ProductsCreate,
        [typeof(UpdateProductCommand)] = InventoryPermissions.ProductsUpdate,
        [typeof(ActivateProductCommand)] = InventoryPermissions.ProductsUpdate,
        [typeof(DeactivateProductCommand)] = InventoryPermissions.ProductsUpdate,
        [typeof(GetProductByIdQuery)] = InventoryPermissions.ProductsRead,
        [typeof(ListProductsQuery)] = InventoryPermissions.ProductsRead,
        [typeof(CreateWarehouseCommand)] = InventoryPermissions.WarehousesCreate,
        [typeof(UpdateWarehouseCommand)] = InventoryPermissions.WarehousesUpdate,
        [typeof(ActivateWarehouseCommand)] = InventoryPermissions.WarehousesUpdate,
        [typeof(DeactivateWarehouseCommand)] = InventoryPermissions.WarehousesUpdate,
        [typeof(GetWarehouseByIdQuery)] = InventoryPermissions.WarehousesRead,
        [typeof(ListWarehousesQuery)] = InventoryPermissions.WarehousesRead,
        [typeof(ReceiveStockCommand)] = InventoryPermissions.StockUpdate,
        [typeof(ReserveStockCommand)] = InventoryPermissions.StockUpdate,
        [typeof(ReleaseReservationCommand)] = InventoryPermissions.StockUpdate,
        [typeof(AdjustStockCommand)] = InventoryPermissions.StockUpdate,
        [typeof(GetInventoryItemByIdQuery)] = InventoryPermissions.StockRead,
        [typeof(ListInventoryItemsQuery)] = InventoryPermissions.StockRead,
    };

    [Fact]
    public void EveryInventoryRequest_ShouldDeclareExactlyOnePermission()
    {
        foreach ((Type request, string expected) in Expected)
        {
            RequirePermissionAttribute[] attributes = request
                .GetCustomAttributes<RequirePermissionAttribute>(inherit: true)
                .ToArray();

            attributes.Should().ContainSingle($"{request.Name} must declare exactly one permission.");
            attributes[0].Permission.Should().Be(expected);
        }
    }

    [Fact]
    public void EveryInventoryRequest_ShouldBeCoveredByThePermissionMap()
    {
        Type[] requestTypes = typeof(CreateProductCommand).Assembly
            .GetTypes()
            .Where(type => type.IsClass
                && (type.Name.EndsWith("Command", StringComparison.Ordinal)
                    || type.Name.EndsWith("Query", StringComparison.Ordinal)))
            .ToArray();

        requestTypes.Should().BeEquivalentTo(Expected.Keys);
    }

    [Fact]
    public void PermissionCodes_ShouldBeStableAndUnique()
    {
        string[] codes =
        [
            InventoryPermissions.ProductsRead,
            InventoryPermissions.ProductsCreate,
            InventoryPermissions.ProductsUpdate,
            InventoryPermissions.WarehousesRead,
            InventoryPermissions.WarehousesCreate,
            InventoryPermissions.WarehousesUpdate,
            InventoryPermissions.StockRead,
            InventoryPermissions.StockUpdate,
        ];

        codes.Should().OnlyHaveUniqueItems();
        codes.Should().OnlyContain(code => code.StartsWith("inventory.", StringComparison.Ordinal));
        codes.Should().BeEquivalentTo(Expected.Values.Distinct());
    }

    [Fact]
    public void PermissionCatalog_ShouldDeclareEveryStableCode()
    {
        string[] codes =
        [
            InventoryPermissions.ProductsRead,
            InventoryPermissions.ProductsCreate,
            InventoryPermissions.ProductsUpdate,
            InventoryPermissions.WarehousesRead,
            InventoryPermissions.WarehousesCreate,
            InventoryPermissions.WarehousesUpdate,
            InventoryPermissions.StockRead,
            InventoryPermissions.StockUpdate,
        ];

        var catalog = new InventoryPermissionCatalog();

        catalog.Permissions.Select(permission => permission.Code).Should().BeEquivalentTo(codes);
        catalog.Permissions.Should().OnlyContain(permission => !string.IsNullOrWhiteSpace(permission.Description));
    }

}

