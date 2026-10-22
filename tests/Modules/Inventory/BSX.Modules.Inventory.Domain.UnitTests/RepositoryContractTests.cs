using System.Reflection;
using BSX.Modules.Inventory.Domain.InventoryItems;
using BSX.Modules.Inventory.Domain.Products;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.Modules.Inventory.Domain.Warehouses;

namespace BSX.Modules.Inventory.Domain.UnitTests;

/// <summary>
/// Locks tenant read isolation for Inventory: every read contract starts with a
/// <see cref="TenantId"/>, so cross-tenant reads cannot be expressed by accident.
/// </summary>
public sealed class RepositoryContractTests
{
    [Fact]
    public void Product_ReadContract_Should_RequireTenant()
    {
        ParameterInfo[] parameters = typeof(IProductRepository)
            .GetMethod(nameof(IProductRepository.GetByIdAsync))!
            .GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(ProductId));
    }

    [Fact]
    public void Product_SkuLookup_Should_BeTenantScoped()
    {
        ParameterInfo[] parameters = typeof(IProductRepository)
            .GetMethod(nameof(IProductRepository.ExistsBySkuAsync))!
            .GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(Sku));
    }

    [Fact]
    public void Warehouse_ReadContract_Should_RequireTenant()
    {
        ParameterInfo[] parameters = typeof(IWarehouseRepository)
            .GetMethod(nameof(IWarehouseRepository.GetByIdAsync))!
            .GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(WarehouseId));
    }

    [Fact]
    public void InventoryItem_ReadContract_Should_RequireTenant()
    {
        ParameterInfo[] parameters = typeof(IInventoryItemRepository)
            .GetMethod(nameof(IInventoryItemRepository.GetByIdAsync))!
            .GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(InventoryItemId));
    }

    [Fact]
    public void InventoryItem_ProductWarehouseLookup_Should_BeTenantScoped()
    {
        ParameterInfo[] parameters = typeof(IInventoryItemRepository)
            .GetMethod(nameof(IInventoryItemRepository.GetByProductAndWarehouseAsync))!
            .GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(ProductId));
        parameters[2].ParameterType.Should().Be(typeof(WarehouseId));
    }

    [Fact]
    public void InventoryItem_UniquenessLookup_Should_BeTenantScoped()
    {
        ParameterInfo[] parameters = typeof(IInventoryItemRepository)
            .GetMethod(nameof(IInventoryItemRepository.ExistsByProductAndWarehouseAsync))!
            .GetParameters();

        parameters[0].ParameterType.Should().Be(typeof(TenantId));
        parameters[1].ParameterType.Should().Be(typeof(ProductId));
        parameters[2].ParameterType.Should().Be(typeof(WarehouseId));
    }

    [Fact]
    public void Repositories_Should_BeAggregateSpecific()
    {
        typeof(IProductRepository).GetMethods().Should().Contain(method => method.Name == "AddAsync");
        typeof(IWarehouseRepository).GetMethods().Should().Contain(method => method.Name == "AddAsync");
        typeof(IInventoryItemRepository).GetMethods().Should().Contain(method => method.Name == "AddAsync");
    }
}
