using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Memberships.AcceptMembership;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Memberships;

public sealed class AcceptMembershipCommandHandlerTests
{
    private readonly InMemoryTenantMembershipRepository _memberships = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TestTimeProvider _timeProvider = new(TestData.Now);

    private AcceptMembershipCommandHandler CreateHandler(TenantId activeTenant) => new(
        _memberships,
        _unitOfWork,
        new TenantGuard(new FakeTenantContext(activeTenant.Value)),
        _timeProvider);

    [Fact]
    public async Task Should_AcceptMembership_WhenTenantMatches()
    {
        TenantId tenantId = TenantId.New();
        TenantMembership membership = CreateInvited(tenantId);
        _memberships.Seed(membership);

        var result = await CreateHandler(tenantId)
            .HandleAsync(new AcceptMembershipCommand(membership.Id.Value));

        result.IsSuccess.Should().BeTrue();
        membership.Status.Should().Be(MembershipStatus.Active);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_FailClosed_WhenMembershipBelongsToAnotherTenant()
    {
        TenantId otherTenant = TenantId.New();
        TenantMembership membership = CreateInvited(otherTenant);
        _memberships.Seed(membership);

        var result = await CreateHandler(TenantId.New())
            .HandleAsync(new AcceptMembershipCommand(membership.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authorization.TenantForbidden");
        membership.Status.Should().Be(MembershipStatus.Invited);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    private static TenantMembership CreateInvited(TenantId tenantId)
        => TenantMembership.Invite(MembershipId.New(), tenantId, UserId.New(), null, TestData.Now).Value;
}
