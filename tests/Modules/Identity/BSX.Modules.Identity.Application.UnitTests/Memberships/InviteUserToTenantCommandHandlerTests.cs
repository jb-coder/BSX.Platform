using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Memberships.InviteUserToTenant;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Memberships;

public sealed class InviteUserToTenantCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryTenantMembershipRepository _memberships = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TestTimeProvider _timeProvider = new(TestData.Now);

    private InviteUserToTenantCommandHandler CreateHandler(Guid activeTenantId)
        => new(_users, _memberships, _unitOfWork, _timeProvider, new TenantGuard(new FakeTenantContext(activeTenantId)));

    [Fact]
    public async Task Should_Fail_WhenTenantMismatch()
    {
        var activeTenant = Guid.NewGuid();

        var result = await CreateHandler(activeTenant).HandleAsync(new InviteUserToTenantCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.TenantForbidden");
    }

    [Fact]
    public async Task Should_Fail_WhenUserNotFound()
    {
        var tenantId = Guid.NewGuid();

        var result = await CreateHandler(tenantId).HandleAsync(new InviteUserToTenantCommand(tenantId, Guid.NewGuid(), null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotFound");
    }

    [Fact]
    public async Task Should_Fail_WhenAlreadyMember()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);
        var tenantId = TenantId.From(Guid.NewGuid());
        _memberships.Seed(TestData.CreateActiveMembership(tenantId, user.Id));

        var result = await CreateHandler(tenantId.Value).HandleAsync(new InviteUserToTenantCommand(tenantId.Value, user.Id.Value, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Membership.AlreadyExists");
    }

    [Fact]
    public async Task Should_Invite_AndPersist()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);
        var tenantId = Guid.NewGuid();

        var result = await CreateHandler(tenantId).HandleAsync(new InviteUserToTenantCommand(tenantId, user.Id.Value, null));

        result.IsSuccess.Should().BeTrue();
        _memberships.Memberships.Should().ContainSingle();
        _memberships.Memberships.Single().Status.Should().Be(MembershipStatus.Invited);
    }
}
