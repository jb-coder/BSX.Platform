using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Memberships.ChangeMembershipRole;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Memberships;

public sealed class ChangeMembershipRoleCommandHandlerTests
{
    private readonly InMemoryTenantMembershipRepository _memberships = new();
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ChangeMembershipRoleCommandHandler CreateHandler(Guid activeTenantId)
        => new(_memberships, _roles, _unitOfWork, new TenantGuard(new FakeTenantContext(activeTenantId)));

    [Fact]
    public async Task Should_Change_ToTenantRole()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var membership = TestData.CreateActiveMembership(tenantId, UserId.New());
        Role role = TestData.CreateRole(tenantId: tenantId, name: "Manager");
        _memberships.Seed(membership);
        _roles.Seed(role);

        var result = await CreateHandler(tenantId.Value).HandleAsync(
            new ChangeMembershipRoleCommand(membership.Id.Value, role.Id.Value));

        result.IsSuccess.Should().BeTrue();
        membership.RoleIds.Should().Contain(role.Id);
    }

    [Fact]
    public async Task Should_RejectSystemRole()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var membership = TestData.CreateActiveMembership(tenantId, UserId.New());
        Role role = TestData.CreateRole(tenantId: null, name: "Administrator", isSystem: true);
        _memberships.Seed(membership);
        _roles.Seed(role);

        var result = await CreateHandler(tenantId.Value).HandleAsync(
            new ChangeMembershipRoleCommand(membership.Id.Value, role.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.NotAssignableToMembership");
        membership.RoleIds.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_RejectRoleFromAnotherTenant()
    {
        var tenantA = TenantId.From(Guid.NewGuid());
        var membership = TestData.CreateActiveMembership(tenantA, UserId.New());
        Role role = TestData.CreateRole(tenantId: TenantId.From(Guid.NewGuid()), name: "Other");
        _memberships.Seed(membership);
        _roles.Seed(role);

        var result = await CreateHandler(tenantA.Value).HandleAsync(
            new ChangeMembershipRoleCommand(membership.Id.Value, role.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.TenantMismatch");
        membership.RoleIds.Should().BeEmpty();
    }
}
