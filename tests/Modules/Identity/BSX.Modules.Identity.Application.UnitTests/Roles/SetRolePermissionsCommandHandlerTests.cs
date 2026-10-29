using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Roles.SetRolePermissions;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Roles;

public sealed class SetRolePermissionsCommandHandlerTests
{
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private SetRolePermissionsCommandHandler CreateHandler(Guid? activeTenantId)
        => new(_roles, _unitOfWork, new TenantGuard(new FakeTenantContext(activeTenantId)));

    [Fact]
    public async Task Should_ReplacePermissions()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        Role role = TestData.CreateRole(tenantId: tenantId, permission: "crm.customers.read");
        _roles.Seed(role);

        var result = await CreateHandler(tenantId.Value).HandleAsync(
            new SetRolePermissionsCommand(role.Id.Value, ["crm.customers.create", "crm.orders.read"]));

        result.IsSuccess.Should().BeTrue();
        role.Permissions.Select(permission => permission.Code)
            .Should().BeEquivalentTo(["crm.customers.create", "crm.orders.read"]);
    }

    [Fact]
    public async Task Should_Fail_WhenCrossTenant()
    {
        Role role = TestData.CreateRole(tenantId: TenantId.From(Guid.NewGuid()), permission: "crm.customers.read");
        _roles.Seed(role);

        var result = await CreateHandler(Guid.NewGuid()).HandleAsync(
            new SetRolePermissionsCommand(role.Id.Value, ["crm.customers.create"]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.TenantForbidden");
    }

    [Fact]
    public async Task Should_RejectSystemRole()
    {
        Role role = TestData.CreateRole(tenantId: null, name: "Administrator", isSystem: true);
        _roles.Seed(role);

        var result = await CreateHandler(null).HandleAsync(
            new SetRolePermissionsCommand(role.Id.Value, ["crm.customers.create"]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.SystemImmutable");
    }

    [Fact]
    public async Task Should_Fail_WhenRoleNotFound()
    {
        var result = await CreateHandler(Guid.NewGuid()).HandleAsync(
            new SetRolePermissionsCommand(Guid.NewGuid(), ["crm.customers.read"]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.NotFound");
    }
}
