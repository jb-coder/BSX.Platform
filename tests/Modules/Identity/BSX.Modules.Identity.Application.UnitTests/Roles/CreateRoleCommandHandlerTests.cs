using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Roles.CreateRole;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Roles;

public sealed class CreateRoleCommandHandlerTests
{
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CreateRoleCommandHandler CreateHandler(Guid? activeTenantId = null) => new(
        _roles,
        _unitOfWork,
        TestPermissionCatalog.Platform,
        new TenantGuard(new FakeTenantContext(activeTenantId ?? _tenantId)));

    [Fact]
    public async Task Should_CreateRole_AndPersist()
    {
        var result = await CreateHandler().HandleAsync(new CreateRoleCommand(_tenantId, "Manager", "Manages things", ["crm.customers.read"]));

        result.IsSuccess.Should().BeTrue();
        result.Value.RoleId.Should().NotBeEmpty();
        _roles.Roles.Single().Permissions.Should().ContainSingle();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenNameAlreadyExists()
    {
        _roles.Seed(TestData.CreateRole(tenantId: TenantId.From(_tenantId), name: "Manager"));

        var result = await CreateHandler().HandleAsync(new CreateRoleCommand(_tenantId, "Manager", null, []));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.NameAlreadyExists");
    }

    [Fact]
    public async Task Should_Fail_WhenPermissionInvalid()
    {
        var result = await CreateHandler().HandleAsync(new CreateRoleCommand(_tenantId, "Manager", null, ["INVALID"]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Permission.Invalid");
    }

    [Fact]
    public async Task Should_RejectPlatformPermission_OnATenantRole()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateRoleCommand(_tenantId, "Manager", null, ["identity.users.read"]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Permission.ScopeForbidden");
        _roles.Roles.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_RejectUnknownPermission()
    {
        var result = await CreateHandler().HandleAsync(
            new CreateRoleCommand(_tenantId, "Manager", null, ["crm.orders.read"]));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Permission.Unknown");
        _roles.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_RejectPlatformRoleCreation()
    {
        var result = await CreateHandler(activeTenantId: null).HandleAsync(
            new CreateRoleCommand(null, "Platform Manager", null, []));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.PlatformRoleCreationForbidden");
        _roles.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_RejectRoleCreation_ForAnotherTenant()
    {
        var result = await CreateHandler(activeTenantId: Guid.NewGuid()).HandleAsync(
            new CreateRoleCommand(_tenantId, "Manager", null, []));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.TenantForbidden");
        _roles.Roles.Should().BeEmpty();
    }
}
