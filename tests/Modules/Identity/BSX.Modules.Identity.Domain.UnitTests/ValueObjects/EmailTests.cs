using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.UnitTests.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void Should_Normalize_WhenValid()
    {
        var result = Email.Create("  User@Example.COM ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("a@@example.com")]
    [InlineData("a b@example.com")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData(null)]
    public void Should_Fail_WhenInvalid(string? value)
    {
        Email.Create(value).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_BeEqual_WhenValuesDifferOnlyByCase()
    {
        Email.Create("user@example.com").Value.Should().Be(Email.Create("USER@example.com").Value);
    }
}
