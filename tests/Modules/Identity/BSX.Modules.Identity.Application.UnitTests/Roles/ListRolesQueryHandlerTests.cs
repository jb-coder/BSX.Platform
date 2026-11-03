using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Roles.ListRoles;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Roles;

public sealed class ListRolesQueryHandlerTests
{
    private readonly InMemoryRoleReadRepository _roles = new();

    private ListRolesQueryHandler CreateHandler(TenantId activeTenant) => new(
        _roles,
        new TenantGuard(new FakeTenantContext(activeTenant.Value)));

    [Fact]
    public async Task Should_ListRoles_ForTheActiveTenant()
    {
        TenantId tenantId = TenantId.New();
        _roles.Seed(new RoleDto(Guid.NewGuid(), tenantId.Value, "Manager", null, false, []));

        var result = await CreateHandler(tenantId).HandleAsync(new ListRolesQuery(tenantId.Value));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(role => role.Name == "Manager");
    }

    [Fact]
    public async Task Should_FailClosed_ForAnotherTenant()
    {
        TenantId otherTenant = TenantId.New();

        var result = await CreateHandler(TenantId.New()).HandleAsync(new ListRolesQuery(otherTenant.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.TenantForbidden");
    }

    [Fact]
    public async Task Should_Allow_SystemRoleListing()
    {
        _roles.Seed(new RoleDto(Guid.NewGuid(), null, "Administrator", null, true, []));

        var result = await CreateHandler(TenantId.New()).HandleAsync(new ListRolesQuery(null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(role => role.IsSystem && role.Name == "Administrator");
    }
}
