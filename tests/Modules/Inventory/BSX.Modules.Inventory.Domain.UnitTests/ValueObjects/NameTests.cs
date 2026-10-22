using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.UnitTests.ValueObjects;

public sealed class NameTests
{
    [Fact]
    public void ProductName_Should_TrimAndPreserveCase()
    {
        ProductName name = ProductName.Create("  Widget Pro  ").Value;

        name.Value.Should().Be("Widget Pro");
    }

    [Fact]
    public void ProductName_Should_RejectEmptyValues()
    {
        var result = ProductName.Create("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.ProductNameInvalid);
    }

    [Fact]
    public void ProductName_Should_RejectValuesLongerThanTheMaximum()
    {
        var result = ProductName.Create(new string('A', ProductName.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.ProductNameInvalid);
    }

    [Fact]
    public void WarehouseName_Should_TrimAndPreserveCase()
    {
        WarehouseName name = WarehouseName.Create("  East Depot  ").Value;

        name.Value.Should().Be("East Depot");
    }

    [Fact]
    public void WarehouseName_Should_RejectEmptyValues()
    {
        var result = WarehouseName.Create(null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.WarehouseNameInvalid);
    }

    [Fact]
    public void WarehouseName_Should_RejectValuesLongerThanTheMaximum()
    {
        var result = WarehouseName.Create(new string('A', WarehouseName.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.WarehouseNameInvalid);
    }

    [Fact]
    public void Names_Should_BeEqual_WhenNormalizedValuesMatch()
    {
        ProductName.Create("Widget").Value.Should().Be(ProductName.Create(" Widget ").Value);
        WarehouseName.Create("Depot").Value.Should().Be(WarehouseName.Create(" Depot ").Value);
    }
}
