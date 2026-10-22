using BSX.Modules.Inventory.Domain.DomainEvents;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.InventoryItems;
using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.UnitTests.InventoryItems;

public sealed class InventoryItemStockTests
{
    [Fact]
    public void Should_CreateAnEmptyStockPosition()
    {
        ProductId productId = ProductId.New();
        WarehouseId warehouseId = WarehouseId.New();
        InventoryItemId id = InventoryItemId.New();

        InventoryItem item = InventoryItem.Create(id, TestData.Tenant, productId, warehouseId).Value;

        item.Id.Should().Be(id);
        item.TenantId.Should().Be(TestData.Tenant);
        item.ProductId.Should().Be(productId);
        item.WarehouseId.Should().Be(warehouseId);
        item.OnHandQuantity.Should().Be(Quantity.Zero);
        item.ReservedQuantity.Should().Be(Quantity.Zero);
        item.AvailableQuantity.Should().Be(Quantity.Zero);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_ReceiveStock_AndRaiseStockReceived()
    {
        InventoryItem item = TestData.CreateItem();

        item.Receive(TestData.QuantityOf(25)).IsSuccess.Should().BeTrue();

        item.OnHandQuantity.Value.Should().Be(25);
        item.ReservedQuantity.Value.Should().Be(0);
        item.AvailableQuantity.Value.Should().Be(25);

        StockReceived received = item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReceived>().Subject;
        received.InventoryItemId.Should().Be(item.Id);
        received.Quantity.Value.Should().Be(25);
    }

    [Fact]
    public void Should_AccumulateReceivedStock()
    {
        InventoryItem item = TestData.CreateItem();

        item.Receive(TestData.QuantityOf(10)).IsSuccess.Should().BeTrue();
        item.Receive(TestData.QuantityOf(15)).IsSuccess.Should().BeTrue();

        item.OnHandQuantity.Value.Should().Be(25);
        item.AvailableQuantity.Value.Should().Be(25);
    }

    [Fact]
    public void Should_RejectReceivingZero()
    {
        InventoryItem item = TestData.CreateItem();

        var result = item.Receive(Quantity.Zero);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockQuantityNotPositive);
        item.OnHandQuantity.Should().Be(Quantity.Zero);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RejectReceivingBeyondTheQuantityRange()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(Quantity.From(Quantity.MaxValue)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Receive(TestData.QuantityOf(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockQuantityOverflow);
        item.OnHandQuantity.Value.Should().Be(Quantity.MaxValue);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_AdjustStockUpAndDown_AndRaiseStockAdjusted()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(50)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        item.Adjust(20).IsSuccess.Should().BeTrue();
        item.OnHandQuantity.Value.Should().Be(70);

        StockAdjusted increased = item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockAdjusted>().Subject;
        increased.Delta.Should().Be(20);
        increased.OnHandQuantity.Value.Should().Be(70);

        item.ClearDomainEvents();
        item.Adjust(-15).IsSuccess.Should().BeTrue();
        item.OnHandQuantity.Value.Should().Be(55);

        StockAdjusted decreased = item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockAdjusted>().Subject;
        decreased.Delta.Should().Be(-15);
        decreased.OnHandQuantity.Value.Should().Be(55);
    }

    [Fact]
    public void Should_RejectAdjustmentToZero()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(10)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Adjust(0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockAdjustmentIsZero);
        item.OnHandQuantity.Value.Should().Be(10);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RejectAdjustmentThatWouldMakeOnHandNegative()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(10)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Adjust(-11);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockAdjustmentWouldBeNegative);
        item.OnHandQuantity.Value.Should().Be(10);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RejectAdjustmentBeyondTheQuantityRange()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(Quantity.From(Quantity.MaxValue)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Adjust(1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockQuantityOverflow);
        item.OnHandQuantity.Value.Should().Be(Quantity.MaxValue);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_NeverExposeNegativeAvailableQuantity()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(10)).IsSuccess.Should().BeTrue();
        item.Reserve(TestData.QuantityOf(10)).IsSuccess.Should().BeTrue();

        item.AvailableQuantity.Value.Should().Be(0);
        item.OnHandQuantity.Value.Should().BeGreaterThanOrEqualTo(item.ReservedQuantity.Value);
    }

    [Fact]
    public void Should_KeepTenantProductAndWarehouseOwnership_AcrossStockMovements()
    {
        TenantId tenant = TenantId.New();
        ProductId product = ProductId.New();
        WarehouseId warehouse = WarehouseId.New();
        InventoryItem item = TestData.CreateItem(product, warehouse, tenant);

        item.Receive(TestData.QuantityOf(10)).IsSuccess.Should().BeTrue();
        item.Reserve(TestData.QuantityOf(4)).IsSuccess.Should().BeTrue();
        item.Release(TestData.QuantityOf(1)).IsSuccess.Should().BeTrue();
        item.Adjust(-2).IsSuccess.Should().BeTrue();

        item.TenantId.Should().Be(tenant);
        item.ProductId.Should().Be(product);
        item.WarehouseId.Should().Be(warehouse);
    }
}
