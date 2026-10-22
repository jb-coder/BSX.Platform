using BSX.Modules.Inventory.Domain.InventoryItems;
using BSX.Modules.Inventory.Domain.Products;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.Modules.Inventory.Domain.Warehouses;

namespace BSX.Modules.Inventory.Domain.UnitTests;

/// <summary>Shared builders for Inventory domain tests.</summary>
internal static class TestData
{
    internal static readonly TenantId Tenant = TenantId.New();

    internal static Sku SkuOf(string value = "SKU-001") => Sku.Create(value).Value;

    internal static ProductName ProductNameOf(string value = "Widget") => ProductName.Create(value).Value;

    internal static WarehouseName WarehouseNameOf(string value = "Main Warehouse") => WarehouseName.Create(value).Value;

    internal static Quantity QuantityOf(int value = 10) => Quantity.Create(value).Value;

    internal static Product CreateProduct(
        string sku = "SKU-001",
        string name = "Widget",
        TenantId? tenantId = null)
    {
        Product product = Product.Create(ProductId.New(), tenantId ?? Tenant, SkuOf(sku), ProductNameOf(name)).Value;
        product.ClearDomainEvents();
        return product;
    }

    internal static Warehouse CreateWarehouse(
        string name = "Main Warehouse",
        TenantId? tenantId = null)
    {
        Warehouse warehouse = Warehouse.Create(WarehouseId.New(), tenantId ?? Tenant, WarehouseNameOf(name)).Value;
        warehouse.ClearDomainEvents();
        return warehouse;
    }

    internal static InventoryItem CreateItem(
        ProductId? productId = null,
        WarehouseId? warehouseId = null,
        TenantId? tenantId = null)
    {
        InventoryItem item = InventoryItem.Create(
            InventoryItemId.New(),
            tenantId ?? Tenant,
            productId ?? ProductId.New(),
            warehouseId ?? WarehouseId.New()).Value;

        item.ClearDomainEvents();
        return item;
    }
}
