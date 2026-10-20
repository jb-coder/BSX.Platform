using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.ValueObjects;

public sealed class AddressTests
{
    [Fact]
    public void Should_CreateAddress_WhenRequiredFieldsPresent()
    {
        var result = Address.Create("1 Main St", " Suite 4 ", "Springfield", "IL", "62701", "US");

        result.IsSuccess.Should().BeTrue();
        result.Value.Line1.Should().Be("1 Main St");
        result.Value.Line2.Should().Be("Suite 4");
        result.Value.City.Should().Be("Springfield");
        result.Value.Region.Should().Be("IL");
        result.Value.PostalCode.Should().Be("62701");
        result.Value.Country.Should().Be("US");
    }

    [Fact]
    public void Should_NormalizeOptionalFields_WhenBlank()
    {
        var result = Address.Create("1 Main St", "  ", "Springfield", null, " ", "US");

        result.IsSuccess.Should().BeTrue();
        result.Value.Line2.Should().BeNull();
        result.Value.Region.Should().BeNull();
        result.Value.PostalCode.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "Springfield", "US")]
    [InlineData("   ", "Springfield", "US")]
    [InlineData("1 Main St", null, "US")]
    [InlineData("1 Main St", "  ", "US")]
    [InlineData("1 Main St", "Springfield", null)]
    [InlineData("1 Main St", "Springfield", "  ")]
    public void Should_Fail_WhenRequiredFieldMissing(string? line1, string? city, string? country)
    {
        var result = Address.Create(line1, null, city, null, null, country);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.AddressInvalid);
    }

    [Fact]
    public void Should_Fail_WhenFieldTooLong()
    {
        var result = Address.Create(
            new string('a', Address.MaxLineLength + 1),
            null,
            "Springfield",
            null,
            null,
            "US");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.AddressInvalid);
    }

    [Fact]
    public void Should_BeEqual_WhenComponentsMatch()
    {
        TestData.Address().Should().Be(TestData.Address());
    }
}
