using BSX.Modules.Sales.Domain.Errors;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.UnitTests.ValueObjects;

public sealed class OpportunityTitleTests
{
    [Fact]
    public void Should_NormalizeTitle_WhenValid()
    {
        var result = OpportunityTitle.Create("  Enterprise opportunity  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Enterprise opportunity");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Fail_WhenEmpty(string? value)
    {
        var result = OpportunityTitle.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTitleInvalid);
    }

    [Fact]
    public void Should_Fail_WhenTooLong()
    {
        var result = OpportunityTitle.Create(new string('A', OpportunityTitle.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SalesErrors.OpportunityTitleInvalid);
    }

    [Fact]
    public void Should_AcceptMaximumLength()
    {
        OpportunityTitle.Create(new string('A', OpportunityTitle.MaxLength)).IsSuccess.Should().BeTrue();
    }
}
