using BSX.Modules.Inventory.Application.Abstractions;
using BSX.Modules.Inventory.Application.Errors;
using BSX.Modules.Inventory.Application.UnitTests.Fakes;
using BSX.Modules.Inventory.Application.Warehouses.ActivateWarehouse;
using BSX.Modules.Inventory.Application.Warehouses.CreateWarehouse;
using BSX.Modules.Inventory.Application.Warehouses.DeactivateWarehouse;
using BSX.Modules.Inventory.Application.Warehouses.UpdateWarehouse;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.Warehouses;

namespace BSX.Modules.Inventory.Application.UnitTests.Warehouses;

public sealed class WarehouseCommandHandlerTests
{
    private readonly InMemoryWarehouseRepository _warehouses = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Should_CreateWarehouse_AndPersist()
    {
        var result = await CreateHandler().HandleAsync(new CreateWarehouseCommand("  Main Warehouse  "));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        Warehouse warehouse = _warehouses.Warehouses.Should().ContainSingle().Subject;
        warehouse.Id.Value.Should().Be(result.Value);
        warehouse.Name.Value.Should().Be("Main Warehouse");
        warehouse.TenantId.Value.Should().Be(TestData.Tenant);
        warehouse.IsOperational.Should().BeTrue();
        _warehouses.LastRequestedTenant.Should().BeNull();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenWarehouseNameIsInvalid()
    {
        var result = await CreateHandler().HandleAsync(new CreateWarehouseCommand(" "));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.WarehouseNameInvalid);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenTenantIsMissing()
    {
        var handler = new CreateWarehouseCommandHandler(
            _warehouses,
            _unitOfWork,
            new InventoryTenantResolver(new FakeTenantContext(null)));

        var result = await handler.HandleAsync(new CreateWarehouseCommand("Main Warehouse"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.TenantRequired);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_UpdateWarehouseName_UsingTenantScope()
    {
        Guid warehouseId = Guid.NewGuid();
        Warehouse warehouse = TestData.CreateWarehouse(id: warehouseId);
        _warehouses.Seed(warehouse);

        var result = await UpdateHandler().HandleAsync(new UpdateWarehouseCommand(warehouseId, "Renamed"));

        result.IsSuccess.Should().BeTrue();
        warehouse.Name.Value.Should().Be("Renamed");
        _warehouses.LastRequestedTenant!.Value.Should().Be(TestData.Tenant);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingUnknownWarehouse()
    {
        var result = await UpdateHandler().HandleAsync(new UpdateWarehouseCommand(Guid.NewGuid(), "Renamed"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.WarehouseNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenWarehouseBelongsToAnotherTenant()
    {
        Warehouse warehouse = TestData.CreateWarehouse(tenantId: Guid.NewGuid());
        _warehouses.Seed(warehouse);

        var result = await UpdateHandler().HandleAsync(
            new UpdateWarehouseCommand(warehouse.Id.Value, "Renamed"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.WarehouseNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_WhenUpdatingWithEmptyWarehouseId()
    {
        var result = await UpdateHandler().HandleAsync(new UpdateWarehouseCommand(Guid.Empty, "Renamed"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryApplicationErrors.InvalidWarehouseId);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_ActivateInactiveWarehouse()
    {
        Guid warehouseId = Guid.NewGuid();
        Warehouse warehouse = TestData.CreateWarehouse(id: warehouseId, active: false);
        _warehouses.Seed(warehouse);

        var result = await ActivateHandler().HandleAsync(new ActivateWarehouseCommand(warehouseId));

        result.IsSuccess.Should().BeTrue();
        warehouse.IsOperational.Should().BeTrue();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenActivatingAnActiveWarehouse()
    {
        Guid warehouseId = Guid.NewGuid();
        _warehouses.Seed(TestData.CreateWarehouse(id: warehouseId));

        var result = await ActivateHandler().HandleAsync(new ActivateWarehouseCommand(warehouseId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.WarehouseAlreadyActive);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_DeactivateActiveWarehouse()
    {
        Guid warehouseId = Guid.NewGuid();
        Warehouse warehouse = TestData.CreateWarehouse(id: warehouseId);
        _warehouses.Seed(warehouse);

        var result = await DeactivateHandler().HandleAsync(new DeactivateWarehouseCommand(warehouseId));

        result.IsSuccess.Should().BeTrue();
        warehouse.IsOperational.Should().BeFalse();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Propagate_WhenDeactivatingAnInactiveWarehouse()
    {
        Guid warehouseId = Guid.NewGuid();
        _warehouses.Seed(TestData.CreateWarehouse(id: warehouseId, active: false));

        var result = await DeactivateHandler().HandleAsync(new DeactivateWarehouseCommand(warehouseId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.WarehouseAlreadyInactive);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task WarehouseLifecycleCommands_Should_Fail_WhenWarehouseIsNotFound()
    {
        Guid warehouseId = Guid.NewGuid();

        var activated = await ActivateHandler().HandleAsync(new ActivateWarehouseCommand(warehouseId));
        var deactivated = await DeactivateHandler().HandleAsync(new DeactivateWarehouseCommand(warehouseId));

        activated.Error.Should().Be(InventoryApplicationErrors.WarehouseNotFound);
        deactivated.Error.Should().Be(InventoryApplicationErrors.WarehouseNotFound);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    private InventoryTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));

    private CreateWarehouseCommandHandler CreateHandler()
        => new(_warehouses, _unitOfWork, TenantResolver());

    private UpdateWarehouseCommandHandler UpdateHandler()
        => new(_warehouses, _unitOfWork, TenantResolver());

    private ActivateWarehouseCommandHandler ActivateHandler()
        => new(_warehouses, _unitOfWork, TenantResolver());

    private DeactivateWarehouseCommandHandler DeactivateHandler()
        => new(_warehouses, _unitOfWork, TenantResolver());
}
