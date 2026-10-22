using BSX.Modules.Inventory.Domain.DomainEvents;
using BSX.Modules.Inventory.Domain.Enums;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.Modules.Inventory.Domain.Warehouses;

namespace BSX.Modules.Inventory.Domain.UnitTests.Warehouses;

public sealed class WarehouseTests
{
    [Fact]
    public void Should_CreateAnOperationalWarehouse_AndRaiseWarehouseCreated()
    {
        WarehouseId id = WarehouseId.New();
        WarehouseName name = TestData.WarehouseNameOf("Main Warehouse");

        Warehouse warehouse = Warehouse.Create(id, TestData.Tenant, name).Value;

        warehouse.Id.Should().Be(id);
        warehouse.TenantId.Should().Be(TestData.Tenant);
        warehouse.Name.Should().Be(name);
        warehouse.Status.Should().Be(WarehouseStatus.Active);
        warehouse.IsOperational.Should().BeTrue();

        WarehouseCreated created = warehouse.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WarehouseCreated>().Subject;
        created.WarehouseId.Should().Be(id);
        created.Name.Should().Be(name);
    }

    [Fact]
    public void Should_DeactivateAndActivate_AndRaiseTheLifecycleEvents()
    {
        Warehouse warehouse = TestData.CreateWarehouse();

        warehouse.Deactivate().IsSuccess.Should().BeTrue();
        warehouse.Status.Should().Be(WarehouseStatus.Inactive);
        warehouse.IsOperational.Should().BeFalse();
        warehouse.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<WarehouseDeactivated>();

        warehouse.ClearDomainEvents();

        warehouse.Activate().IsSuccess.Should().BeTrue();
        warehouse.Status.Should().Be(WarehouseStatus.Active);
        warehouse.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<WarehouseActivated>();
    }

    [Fact]
    public void Should_RejectDeactivatingAnInactiveWarehouse()
    {
        Warehouse warehouse = TestData.CreateWarehouse();
        warehouse.Deactivate().IsSuccess.Should().BeTrue();
        warehouse.ClearDomainEvents();

        var result = warehouse.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.WarehouseAlreadyInactive);
        warehouse.Status.Should().Be(WarehouseStatus.Inactive);
        warehouse.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RejectActivatingAnActiveWarehouse()
    {
        Warehouse warehouse = TestData.CreateWarehouse();

        var result = warehouse.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.WarehouseAlreadyActive);
        warehouse.Status.Should().Be(WarehouseStatus.Active);
        warehouse.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RenameInAnyStatus_WithoutRaisingAnEvent()
    {
        Warehouse warehouse = TestData.CreateWarehouse();
        warehouse.Deactivate().IsSuccess.Should().BeTrue();
        warehouse.ClearDomainEvents();

        var result = warehouse.Rename(TestData.WarehouseNameOf("East Depot"));

        result.IsSuccess.Should().BeTrue();
        warehouse.Name.Value.Should().Be("East Depot");
        warehouse.Status.Should().Be(WarehouseStatus.Inactive);
        warehouse.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_KeepTenantOwnership_AcrossLifecycleChanges()
    {
        TenantId tenant = TenantId.New();
        Warehouse warehouse = TestData.CreateWarehouse(tenantId: tenant);

        warehouse.Deactivate().IsSuccess.Should().BeTrue();
        warehouse.Activate().IsSuccess.Should().BeTrue();

        warehouse.TenantId.Should().Be(tenant);
    }
}
