using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.UnitTests.ValueObjects;

public sealed class QuantityTests
{
    [Fact]
    public void Should_AcceptZero()
    {
        var result = Quantity.Create(0);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Quantity.Zero);
        result.Value.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Should_AcceptTheMaximum()
    {
        var result = Quantity.Create(Quantity.MaxValue);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(Quantity.MaxValue);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(Quantity.MaxValue + 1)]
    public void Should_RejectOutOfRangeValues(int value)
    {
        var result = Quantity.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.QuantityInvalid);
    }

    [Fact]
    public void Should_BeEqual_WhenValuesMatch()
    {
        Quantity.Create(10).Value.Should().Be(Quantity.Create(10).Value);
        Quantity.Create(10).Value.Should().NotBe(Quantity.Create(11).Value);
    }

    [Fact]
    public void Should_RenderInvariantValue()
    {
        Quantity.Create(1234).Value.ToString().Should().Be("1234");
    }
}
