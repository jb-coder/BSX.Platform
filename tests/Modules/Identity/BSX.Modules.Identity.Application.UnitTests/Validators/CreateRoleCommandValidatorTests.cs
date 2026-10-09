using BSX.Modules.Identity.Application.Roles.CreateRole;

namespace BSX.Modules.Identity.Application.UnitTests.Validators;

public sealed class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    [Fact]
    public async Task Should_BeValid_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(new CreateRoleCommand(null, "Manager", null, ["crm.customers.read"]));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_BeInvalid_WhenNameIsEmpty()
    {
        var result = await _validator.ValidateAsync(new CreateRoleCommand(null, string.Empty, null, []));

        result.IsValid.Should().BeFalse();
    }
}
