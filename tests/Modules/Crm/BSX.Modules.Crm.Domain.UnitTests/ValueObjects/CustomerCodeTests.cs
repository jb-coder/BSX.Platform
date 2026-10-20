using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.ValueObjects;

public sealed class CustomerCodeTests
{
    [Fact]
    public void Should_NormalizeCode_WhenValid()
    {
        var result = CustomerCode.Create("  cust-001  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("CUST-001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_Fail_WhenEmpty(string? value)
    {
        var result = CustomerCode.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.CustomerCodeInvalid);
    }

    [Fact]
    public void Should_Fail_WhenTooLong()
    {
        var result = CustomerCode.Create(new string('A', CustomerCode.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.CustomerCodeInvalid);
    }

    [Theory]
    [InlineData("ACME#1")]
    [InlineData("ACME 1")]
    [InlineData("ACME/1")]
    public void Should_Fail_WhenContainsInvalidCharacters(string value)
    {
        CustomerCode.Create(value).IsFailure.Should().BeTrue();
    }
}
