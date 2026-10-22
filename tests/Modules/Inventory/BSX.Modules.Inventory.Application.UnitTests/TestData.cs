using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Domain.InventoryItems;
using BSX.Modules.Inventory.Domain.Products;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.Modules.Inventory.Domain.Warehouses;

namespace BSX.Modules.Inventory.Application.UnitTests;

/// <summary>Shared builders for Inventory application tests.</summary>
internal static class TestData
{
    internal static readonly Guid Tenant = Guid.NewGuid();

    internal static TenantId TenantValue(Guid? tenantId = null) => TenantId.From(tenantId ?? Tenant);

    internal static Sku SkuOf(string value = "SKU-001") => Sku.Create(value).Value;

    internal static ProductName ProductNameOf(string value = "Widget") => ProductName.Create(value).Value;

    internal static WarehouseName WarehouseNameOf(string value = "Main Warehouse")
        => WarehouseName.Create(value).Value;

    internal static Quantity QuantityOf(int value = 10) => Quantity.Create(value).Value;

    internal static Product CreateProduct(
        Guid? id = null,
        string sku = "SKU-001",
        string name = "Widget",
        Guid? tenantId = null,
        bool active = true)
    {
        Product product = Product.Create(
            id is { } value ? ProductId.From(value) : ProductId.New(),
            TenantValue(tenantId),
            SkuOf(sku),
            ProductNameOf(name)).Value;

        if (!active)
        {
            product.Deactivate().IsSuccess.Should().BeTrue();
        }

        product.ClearDomainEvents();
        return product;
    }

    internal static Warehouse CreateWarehouse(
        Guid? id = null,
        string name = "Main Warehouse",
        Guid? tenantId = null,
        bool active = true)
    {
        Warehouse warehouse = Warehouse.Create(
            id is { } value ? WarehouseId.From(value) : WarehouseId.New(),
            TenantValue(tenantId),
            WarehouseNameOf(name)).Value;

        if (!active)
        {
            warehouse.Deactivate().IsSuccess.Should().BeTrue();
        }

        warehouse.ClearDomainEvents();
        return warehouse;
    }

    internal static InventoryItem CreateItem(
        Guid? id = null,
        Guid? productId = null,
        Guid? warehouseId = null,
        Guid? tenantId = null,
        int onHand = 0,
        int reserved = 0)
    {
        InventoryItem item = InventoryItem.Create(
            id is { } value ? InventoryItemId.From(value) : InventoryItemId.New(),
            TenantValue(tenantId),
            productId is { } product ? ProductId.From(product) : ProductId.New(),
            warehouseId is { } warehouse ? WarehouseId.From(warehouse) : WarehouseId.New()).Value;

        if (onHand > 0)
        {
            item.Receive(QuantityOf(onHand)).IsSuccess.Should().BeTrue();
        }

        if (reserved > 0)
        {
            item.Reserve(QuantityOf(reserved)).IsSuccess.Should().BeTrue();
        }

        item.ClearDomainEvents();
        return item;
    }

    internal static ProductDto ProductDto(
        Guid? id = null,
        string sku = "SKU-001",
        string name = "Widget",
        string status = "Active")
        => new(id ?? Guid.NewGuid(), Tenant, sku, name, status);

    internal static ProductSummaryDto ProductSummary(
        Guid? id = null,
        string sku = "SKU-001",
        string name = "Widget",
        string status = "Active")
        => new(id ?? Guid.NewGuid(), sku, name, status);

    internal static WarehouseDto WarehouseDto(
        Guid? id = null,
        string name = "Main Warehouse",
        string status = "Active")
        => new(id ?? Guid.NewGuid(), Tenant, name, status);

    internal static WarehouseSummaryDto WarehouseSummary(
        Guid? id = null,
        string name = "Main Warehouse",
        string status = "Active")
        => new(id ?? Guid.NewGuid(), name, status);

    internal static InventoryItemDto InventoryItemDto(
        Guid? id = null,
        Guid? productId = null,
        Guid? warehouseId = null,
        int onHand = 100,
        int reserved = 20)
        => new(
            id ?? Guid.NewGuid(),
            Tenant,
            productId ?? Guid.NewGuid(),
            warehouseId ?? Guid.NewGuid(),
            onHand,
            reserved,
            onHand - reserved);

    internal static InventoryItemSummaryDto InventoryItemSummary(
        Guid? id = null,
        Guid? productId = null,
        Guid? warehouseId = null,
        int onHand = 100,
        int reserved = 20)
        => new(
            id ?? Guid.NewGuid(),
            productId ?? Guid.NewGuid(),
            warehouseId ?? Guid.NewGuid(),
            onHand,
            reserved,
            onHand - reserved);
}
