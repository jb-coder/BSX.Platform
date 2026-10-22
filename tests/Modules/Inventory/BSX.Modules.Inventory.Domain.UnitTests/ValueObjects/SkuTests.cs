using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.UnitTests.ValueObjects;

public sealed class SkuTests
{
    [Fact]
    public void Should_NormalizeTrimAndUpperCase()
    {
        Sku sku = Sku.Create("  sku-001.2_a  ").Value;

        sku.Value.Should().Be("SKU-001.2_A");
    }

    [Fact]
    public void Should_TreatNormalizedValuesAsEqual()
    {
        Sku.Create("sku-001").Value.Should().Be(Sku.Create(" SKU-001 ").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SKU 001")]
    [InlineData("SKU#001")]
    [InlineData("SKU/001")]
    public void Should_RejectInvalidValues(string? value)
    {
        var result = Sku.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.SkuInvalid);
    }

    [Fact]
    public void Should_RejectValuesLongerThanTheMaximum()
    {
        var result = Sku.Create(new string('A', Sku.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InventoryErrors.SkuInvalid);
    }

    [Fact]
    public void Should_AcceptTheMaximumLength()
    {
        var result = Sku.Create(new string('A', Sku.MaxLength));

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Length.Should().Be(Sku.MaxLength);
    }
}
