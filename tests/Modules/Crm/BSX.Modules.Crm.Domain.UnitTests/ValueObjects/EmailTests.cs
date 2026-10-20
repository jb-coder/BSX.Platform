using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void Should_NormalizeEmail_WhenValid()
    {
        var result = Email.Create("  Contact@Example.COM  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("contact@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@@example.com")]
    [InlineData("user @example.com")]
    public void Should_Fail_WhenInvalid(string? value)
    {
        var result = Email.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.EmailInvalid);
    }

    [Fact]
    public void Should_Fail_WhenTooLong()
    {
        string value = new string('a', Email.MaxLength) + "@example.com";

        Email.Create(value).IsFailure.Should().BeTrue();
    }
}
