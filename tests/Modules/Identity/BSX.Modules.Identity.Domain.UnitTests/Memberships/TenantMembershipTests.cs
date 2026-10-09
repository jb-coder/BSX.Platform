using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.Memberships;

public sealed class TenantMembershipTests
{
    private static TenantMembership CreateInvited()
        => TenantMembership.Invite(MembershipId.New(), TenantId.New(), UserId.New(), null, TestData.Now).Value;

    [Fact]
    public void Should_Invite_AsInvited_AndRaiseEvent()
    {
        var membership = CreateInvited();

        membership.Status.Should().Be(MembershipStatus.Invited);
        membership.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is MembershipInvited);
    }

    [Fact]
    public void Should_Accept_AndActivate()
    {
        var membership = CreateInvited();

        Result result = membership.Accept(TestData.Now);

        result.IsSuccess.Should().BeTrue();
        membership.Status.Should().Be(MembershipStatus.Active);
        membership.JoinedOnUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public void Should_FailSuspend_WhenNotActive()
    {
        var membership = CreateInvited();

        Result result = membership.Suspend(TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Membership.InvalidTransition");
    }

    [Fact]
    public void Should_ClearRoles_WhenRemoved()
    {
        var membership = CreateInvited();
        membership.Accept(TestData.Now);
        membership.AssignRole(RoleId.New());

        Result result = membership.Remove();

        result.IsSuccess.Should().BeTrue();
        membership.Status.Should().Be(MembershipStatus.Removed);
        membership.RoleIds.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_WhenRoleAlreadyAssigned()
    {
        var membership = CreateInvited();
        membership.Accept(TestData.Now);
        var roleId = RoleId.New();
        membership.AssignRole(roleId);

        Result result = membership.AssignRole(roleId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Membership.RoleAlreadyAssigned");
    }

    [Fact]
    public void Should_Fail_WhenRemovingUnassignedRole()
    {
        var membership = CreateInvited();
        membership.Accept(TestData.Now);

        Result result = membership.RemoveRole(RoleId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Membership.RoleNotAssigned");
    }
}
