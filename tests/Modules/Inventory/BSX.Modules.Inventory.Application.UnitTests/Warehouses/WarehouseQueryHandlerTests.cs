using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Dtos;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Application.UnitTests.Fakes;
using BSX.Modules.Inventory.Application.Warehouses.GetWarehouseById;
using BSX.Modules.Inventory.Application.Warehouses.ListWarehouses;

namespace BSX.Modules.Inventory.Application.UnitTests.Warehouses;

public sealed class WarehouseQueryHandlerTests
{
    private readonly FakeWarehouseReadRepository _warehouses = new();

    [Fact]
    public async Task Should_GetWarehouseById_UsingTenantScope()
    {
        Guid warehouseId = Guid.NewGuid();
        WarehouseDto dto = TestData.WarehouseDto(id: warehouseId);
        _warehouses.Warehouse = dto;

        var handler = new GetWarehouseByIdQueryHandler(_warehouses, TenantResolver());
        var result = await handler.HandleAsync(new GetWarehouseByIdQuery(warehouseId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
        _warehouses.LastTenantId.Should().Be(TestData.Tenant);
    }

    [Fact]
    public async Task Should_Fail_WhenWarehouseProjectionNotFound()
    {
        var handler = new GetWarehouseByIdQueryHandler(_warehouses, TenantResolver());
        var result = await handler.HandleAsync(new GetWarehouseByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.WarehouseNotFound);
    }

    [Fact]
    public async Task Should_Fail_WhenWarehouseIdIsEmpty()
    {
        var handler = new GetWarehouseByIdQueryHandler(_warehouses, TenantResolver());
        var result = await handler.HandleAsync(new GetWarehouseByIdQuery(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.InvalidWarehouseId);
        _warehouses.LastTenantId.Should().BeNull();
    }

    [Fact]
    public async Task Should_Fail_ToGetWarehouse_WhenTenantIsMissing()
    {
        var handler = new GetWarehouseByIdQueryHandler(
            _warehouses,
            new InventoryTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new GetWarehouseByIdQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
    }

    [Fact]
    public async Task Should_ListWarehouses_WithTenantAndNormalizedPaging()
    {
        var handler = new ListWarehousesQueryHandler(_warehouses, TenantResolver());

        var result = await handler.HandleAsync(new ListWarehousesQuery(0, 500, "main"));

        result.IsSuccess.Should().BeTrue();
        _warehouses.LastTenantId.Should().Be(TestData.Tenant);
        _warehouses.LastPage.Should().Be(1);
        _warehouses.LastSize.Should().Be(20);
        _warehouses.LastSearch.Should().Be("main");
    }

    [Fact]
    public async Task Should_ListWarehouses_WithRequestedPaging()
    {
        var handler = new ListWarehousesQueryHandler(_warehouses, TenantResolver());

        await handler.HandleAsync(new ListWarehousesQuery(3, 50, null));

        _warehouses.LastPage.Should().Be(3);
        _warehouses.LastSize.Should().Be(50);
    }

    [Fact]
    public async Task Should_Fail_ToListWarehouses_WhenTenantIsMissing()
    {
        var handler = new ListWarehousesQueryHandler(
            _warehouses,
            new InventoryTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new ListWarehousesQuery(1, 20, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
    }

    private InventoryTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));
}
