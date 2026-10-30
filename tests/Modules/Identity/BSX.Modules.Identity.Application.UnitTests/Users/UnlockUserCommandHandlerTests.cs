using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Application.Users.UnlockUser;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;

namespace BSX.Modules.Identity.Application.UnitTests.Users;

public sealed class UnlockUserCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private UnlockUserCommandHandler CreateHandler() => new(_users, _unitOfWork);

    [Fact]
    public async Task Should_UnlockLockedUser_AndRotateSecurityStamp()
    {
        User user = TestData.CreateActiveUser();
        user.Lock(TestData.Now.AddMinutes(15), TestData.Now);
        string lockedStamp = user.SecurityStamp.Value;
        _users.Seed(user);

        var result = await CreateHandler().HandleAsync(new UnlockUserCommand(user.Id.Value));

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEndUtc.Should().BeNull();
        user.SecurityStamp.Value.Should().NotBe(lockedStamp);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenUserNotLocked()
    {
        User user = TestData.CreateActiveUser();
        _users.Seed(user);

        var result = await CreateHandler().HandleAsync(new UnlockUserCommand(user.Id.Value));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotLocked");
    }

    [Fact]
    public async Task Should_Fail_WhenUserNotFound()
    {
        var result = await CreateHandler().HandleAsync(new UnlockUserCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotFound");
    }
}
