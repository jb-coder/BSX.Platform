using BSX.Modules.Inventory.Domain.DomainEvents;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.InventoryItems;
using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.UnitTests.InventoryItems;

public sealed class InventoryItemReservationTests
{
    [Fact]
    public void Should_ReserveStock_AndRaiseStockReserved()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(100)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        item.Reserve(TestData.QuantityOf(30)).IsSuccess.Should().BeTrue();

        item.OnHandQuantity.Value.Should().Be(100);
        item.ReservedQuantity.Value.Should().Be(30);
        item.AvailableQuantity.Value.Should().Be(70);

        StockReserved reserved = item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReserved>().Subject;
        reserved.InventoryItemId.Should().Be(item.Id);
        reserved.Quantity.Value.Should().Be(30);
    }

    [Fact]
    public void Should_RejectReservingMoreThanAvailable()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(100)).IsSuccess.Should().BeTrue();
        item.Reserve(TestData.QuantityOf(80)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Reserve(TestData.QuantityOf(21));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.InsufficientAvailableStock);
        item.OnHandQuantity.Value.Should().Be(100);
        item.ReservedQuantity.Value.Should().Be(80);
        item.AvailableQuantity.Value.Should().Be(20);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RejectReservingZero()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(10)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Reserve(Quantity.Zero);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockQuantityNotPositive);
        item.ReservedQuantity.Should().Be(Quantity.Zero);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_ReleaseReservedStock_AndRaiseStockReleased()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(100)).IsSuccess.Should().BeTrue();
        item.Reserve(TestData.QuantityOf(40)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        item.Release(TestData.QuantityOf(15)).IsSuccess.Should().BeTrue();

        item.OnHandQuantity.Value.Should().Be(100);
        item.ReservedQuantity.Value.Should().Be(25);
        item.AvailableQuantity.Value.Should().Be(75);

        StockReleased released = item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReleased>().Subject;
        released.InventoryItemId.Should().Be(item.Id);
        released.Quantity.Value.Should().Be(15);
    }

    [Fact]
    public void Should_RejectReleasingMoreThanReserved()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(100)).IsSuccess.Should().BeTrue();
        item.Reserve(TestData.QuantityOf(40)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Release(TestData.QuantityOf(41));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.InsufficientReservedStock);
        item.ReservedQuantity.Value.Should().Be(40);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RejectReleasingZero()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(10)).IsSuccess.Should().BeTrue();
        item.Reserve(TestData.QuantityOf(5)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Release(Quantity.Zero);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockQuantityNotPositive);
        item.ReservedQuantity.Value.Should().Be(5);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_RejectAdjustmentBelowTheReservedQuantity()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(100)).IsSuccess.Should().BeTrue();
        item.Reserve(TestData.QuantityOf(80)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Adjust(-30);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.StockAdjustmentBelowReserved);
        item.OnHandQuantity.Value.Should().Be(100);
        item.ReservedQuantity.Value.Should().Be(80);
        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_AllowAdjustmentDownToTheReservedQuantity()
    {
        InventoryItem item = TestData.CreateItem();
        item.Receive(TestData.QuantityOf(100)).IsSuccess.Should().BeTrue();
        item.Reserve(TestData.QuantityOf(80)).IsSuccess.Should().BeTrue();
        item.ClearDomainEvents();

        var result = item.Adjust(-20);

        result.IsSuccess.Should().BeTrue();
        item.OnHandQuantity.Value.Should().Be(80);
        item.ReservedQuantity.Value.Should().Be(80);
        item.AvailableQuantity.Value.Should().Be(0);
    }

    [Fact]
    public void Should_SupportTheFullReservationLifecycle()
    {
        InventoryItem item = TestData.CreateItem();

        item.Receive(TestData.QuantityOf(50)).IsSuccess.Should().BeTrue();
        item.Reserve(TestData.QuantityOf(20)).IsSuccess.Should().BeTrue();
        item.Release(TestData.QuantityOf(5)).IsSuccess.Should().BeTrue();
        item.Adjust(-5).IsSuccess.Should().BeTrue();

        item.OnHandQuantity.Value.Should().Be(45);
        item.ReservedQuantity.Value.Should().Be(15);
        item.AvailableQuantity.Value.Should().Be(30);
        item.DomainEvents.Should().HaveCount(4);
        item.DomainEvents.Select(domainEvent => domainEvent.GetType()).Should().ContainInOrder(
            typeof(StockReceived),
            typeof(StockReserved),
            typeof(StockReleased),
            typeof(StockAdjusted));
    }
}
