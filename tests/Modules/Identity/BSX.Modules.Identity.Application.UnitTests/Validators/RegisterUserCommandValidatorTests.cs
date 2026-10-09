using BSX.Modules.Identity.Application.Users.RegisterUser;

namespace BSX.Modules.Identity.Application.UnitTests.Validators;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public async Task Should_BeValid_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(new RegisterUserCommand("user@example.com", "Test User", "Passw0rd!"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_BeInvalid_WhenEmailIsMalformed()
    {
        var result = await _validator.ValidateAsync(new RegisterUserCommand("not-an-email", "Test User", null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Should_BeInvalid_WhenPasswordIsTooShort()
    {
        var result = await _validator.ValidateAsync(new RegisterUserCommand("user@example.com", "Test User", "short"));

        result.IsValid.Should().BeFalse();
    }
}
