using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.Users;

public sealed class UserPlatformRoleTests
{
    [Fact]
    public void Should_AssignPlatformRole_WhenNotAlreadyAssigned()
    {
        User user = TestData.CreateActiveUser();
        RoleId roleId = RoleId.New();

        Result result = user.AssignPlatformRole(roleId);

        result.IsSuccess.Should().BeTrue();
        user.PlatformRoleIds.Should().Contain(roleId);
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserPlatformRoleAssigned);
    }

    [Fact]
    public void Should_FailAssign_WhenRoleAlreadyAssigned()
    {
        User user = TestData.CreateActiveUser();
        RoleId roleId = RoleId.New();
        user.AssignPlatformRole(roleId);

        Result result = user.AssignPlatformRole(roleId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.PlatformRoleAlreadyAssigned");
    }

    [Fact]
    public void Should_RemovePlatformRole_WhenAssigned()
    {
        User user = TestData.CreateActiveUser();
        RoleId roleId = RoleId.New();
        user.AssignPlatformRole(roleId);

        Result result = user.RemovePlatformRole(roleId);

        result.IsSuccess.Should().BeTrue();
        user.PlatformRoleIds.Should().NotContain(roleId);
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserPlatformRoleRemoved);
    }

    [Fact]
    public void Should_FailRemove_WhenRoleNotAssigned()
    {
        User user = TestData.CreateActiveUser();

        Result result = user.RemovePlatformRole(RoleId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.PlatformRoleNotAssigned");
    }
}
