using BSX.Modules.Identity.Application.Roles.CreateRole;
using BSX.Modules.Identity.Application.UnitTests.Fakes;

namespace BSX.Modules.Identity.Application.UnitTests.Roles;

public sealed class CreateRoleCommandHandlerTests
{
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private CreateRoleCommandHandler CreateHandler() => new(_roles, _unitOfWork);

    [Fact]
    public async Task Should_CreateRole_AndPersist()
    {
        var result = await CreateHandler().HandleAsync(new CreateRoleCommand(null, "Manager", "Manages things", ["crm.customers.read"]));

        result.IsSuccess.Should().BeTrue();
        result.Value.RoleId.Should().NotBeEmpty();
        _roles.Roles.Single().Permissions.Should().ContainSingle();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenNameAlreadyExists()
    {
        _roles.Seed(TestData.CreateRole(name: "Manager"));

        var result = await CreateHandler().HandleAsync(new CreateRoleCommand(null, "Manager", null, []));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.NameAlreadyExists");
    }

    [Fact]
    public async Task Should_Fail_WhenPermissionInvalid()
    {
        var result = await CreateHandler().HandleAsync(new CreateRoleCommand(null, "Manager", null, ["INVALID"]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Permission.Invalid");
    }
}
