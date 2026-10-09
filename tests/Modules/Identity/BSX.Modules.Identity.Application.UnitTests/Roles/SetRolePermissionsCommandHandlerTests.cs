using BSX.Modules.Identity.Application.Roles.SetRolePermissions;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Roles;

namespace BSX.Modules.Identity.Application.UnitTests.Roles;

public sealed class SetRolePermissionsCommandHandlerTests
{
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private SetRolePermissionsCommandHandler CreateHandler() => new(_roles, _unitOfWork);

    [Fact]
    public async Task Should_ReplacePermissions()
    {
        Role role = TestData.CreateRole(permission: "crm.customers.read");
        _roles.Seed(role);

        var result = await CreateHandler().HandleAsync(new SetRolePermissionsCommand(role.Id.Value, ["crm.customers.create", "crm.orders.read"]));

        result.IsSuccess.Should().BeTrue();
        role.Permissions.Select(permission => permission.Code)
            .Should().BeEquivalentTo(["crm.customers.create", "crm.orders.read"]);
    }

    [Fact]
    public async Task Should_Fail_WhenRoleNotFound()
    {
        var result = await CreateHandler().HandleAsync(new SetRolePermissionsCommand(Guid.NewGuid(), ["crm.customers.read"]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.NotFound");
    }
}
