using BSX.Modules.Identity.Application.Memberships.AssignRoleToMembership;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Memberships;

public sealed class AssignRoleToMembershipCommandHandlerTests
{
    private readonly InMemoryTenantMembershipRepository _memberships = new();
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private AssignRoleToMembershipCommandHandler CreateHandler()
        => new(_memberships, _roles, _unitOfWork);

    [Fact]
    public async Task Should_Assign_WhenRoleBelongsToSameTenant()
    {
        var tenantId = TenantId.From(Guid.NewGuid());
        var membership = TestData.CreateActiveMembership(tenantId, UserId.New());
        Role role = TestData.CreateRole(tenantId: tenantId, name: "Manager");
        _memberships.Seed(membership);
        _roles.Seed(role);

        var result = await CreateHandler().HandleAsync(new AssignRoleToMembershipCommand(membership.Id.Value, role.Id.Value));

        result.IsSuccess.Should().BeTrue();
        membership.RoleIds.Should().Contain(role.Id);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenRoleBelongsToAnotherTenant()
    {
        var tenantA = TenantId.From(Guid.NewGuid());
        var tenantB = TenantId.From(Guid.NewGuid());
        var membership = TestData.CreateActiveMembership(tenantA, UserId.New());
        Role role = TestData.CreateRole(tenantId: tenantB, name: "Other");
        _memberships.Seed(membership);
        _roles.Seed(role);

        var result = await CreateHandler().HandleAsync(new AssignRoleToMembershipCommand(membership.Id.Value, role.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Role.TenantMismatch");
    }
}
