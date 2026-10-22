using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.ValueObjects;

public sealed class LeadTitleTests
{
    [Fact]
    public void Should_NormalizeTitle_WhenValid()
    {
        var result = LeadTitle.Create("  Enterprise deal  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Enterprise deal");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Fail_WhenEmpty(string? value)
    {
        var result = LeadTitle.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadTitleInvalid);
    }

    [Fact]
    public void Should_Fail_WhenTooLong()
    {
        var result = LeadTitle.Create(new string('A', LeadTitle.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.LeadTitleInvalid);
    }

    [Fact]
    public void Should_AcceptMaximumLength()
    {
        LeadTitle.Create(new string('A', LeadTitle.MaxLength)).IsSuccess.Should().BeTrue();
    }
}
