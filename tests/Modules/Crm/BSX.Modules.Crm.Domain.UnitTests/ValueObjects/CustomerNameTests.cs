using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.ValueObjects;

public sealed class CustomerNameTests
{
    [Fact]
    public void Should_TrimName_WhenValid()
    {
        var result = CustomerName.Create("  Acme Corporation  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Acme Corporation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Fail_WhenEmpty(string? value)
    {
        var result = CustomerName.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.NameInvalid);
    }

    [Fact]
    public void Should_Fail_WhenTooLong()
    {
        var result = CustomerName.Create(new string('A', CustomerName.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.NameInvalid);
    }
}
