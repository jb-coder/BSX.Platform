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

    private InviteUserToTenantCommandHandler CreateHandler()
        => new(_users, _memberships, _unitOfWork, _timeProvider);

    [Fact]
    public async Task Should_Fail_WhenUserNotFound()
    {
        var result = await CreateHandler().HandleAsync(new InviteUserToTenantCommand(Guid.NewGuid(), Guid.NewGuid(), null));

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

        var result = await CreateHandler().HandleAsync(new InviteUserToTenantCommand(tenantId.Value, user.Id.Value, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Membership.AlreadyExists");
    }

    [Fact]
    public async Task Should_Invite_AndPersist()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);

        var result = await CreateHandler().HandleAsync(new InviteUserToTenantCommand(Guid.NewGuid(), user.Id.Value, null));

        result.IsSuccess.Should().BeTrue();
        _memberships.Memberships.Should().ContainSingle();
        _memberships.Memberships.Single().Status.Should().Be(MembershipStatus.Invited);
    }
}
