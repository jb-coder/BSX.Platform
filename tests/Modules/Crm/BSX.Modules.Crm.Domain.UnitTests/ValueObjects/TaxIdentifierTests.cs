using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.ValueObjects;

public sealed class TaxIdentifierTests
{
    [Fact]
    public void Should_NormalizeTaxIdentifier_WhenValid()
    {
        var result = TaxIdentifier.Create("  us-12.345/6  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("US-12.345/6");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Fail_WhenEmpty(string? value)
    {
        var result = TaxIdentifier.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.TaxIdentifierInvalid);
    }

    [Fact]
    public void Should_Fail_WhenTooLong()
    {
        var result = TaxIdentifier.Create(new string('A', TaxIdentifier.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.TaxIdentifierInvalid);
    }

    [Theory]
    [InlineData("TAX#1")]
    [InlineData("TAX 1")]
    public void Should_Fail_WhenContainsInvalidCharacters(string value)
    {
        TaxIdentifier.Create(value).IsFailure.Should().BeTrue();
    }
}
