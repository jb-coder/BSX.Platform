using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.ValueObjects;

public sealed class PhoneNumberTests
{
    [Theory]
    [InlineData("+1 (555) 010-0100")]
    [InlineData("555-0100")]
    [InlineData("+34 600 123 456")]
    public void Should_CreatePhoneNumber_WhenValid(string value)
    {
        PhoneNumber.Create(value).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("1234")]
    [InlineData("12345abc")]
    [InlineData("call me")]
    public void Should_Fail_WhenInvalid(string? value)
    {
        var result = PhoneNumber.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.PhoneNumberInvalid);
    }

    [Fact]
    public void Should_Fail_WhenTooLong()
    {
        PhoneNumber.Create(new string('1', PhoneNumber.MaxLength + 1)).IsFailure.Should().BeTrue();
    }
}
