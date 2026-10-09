using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.Users;

public sealed class UserLifecycleTests
{
    [Fact]
    public void Should_Deactivate_WhenActive()
    {
        User user = TestData.CreateActiveUser();

        Result result = user.Deactivate();

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Disabled);
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserDeactivated);
    }

    [Fact]
    public void Should_FailDeactivation_WhenAlreadyDisabled()
    {
        User user = TestData.CreateActiveUser();
        user.Deactivate();

        Result result = user.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.AlreadyDeactivated");
    }

    [Fact]
    public void Should_FailLock_WhenEndIsNotInFuture()
    {
        User user = TestData.CreateActiveUser();

        Result result = user.Lock(TestData.Now, TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.LockoutInvalid");
    }

    [Fact]
    public void Should_LockAndUnlock()
    {
        User user = TestData.CreateActiveUser();

        user.Lock(TestData.Now.AddHours(1), TestData.Now).IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Locked);

        user.Unlock().IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
        user.LockoutEndUtc.Should().BeNull();
    }

    [Fact]
    public void Should_FailUnlock_WhenNotLocked()
    {
        User user = TestData.CreateActiveUser();

        Result result = user.Unlock();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NotLocked");
    }

    [Fact]
    public void Should_LockAfterMaxAccessFailures()
    {
        User user = TestData.CreateActiveUser();

        user.RecordAccessFailure(TestData.Now, maxAttempts: 3);
        user.RecordAccessFailure(TestData.Now, maxAttempts: 3);
        user.RecordAccessFailure(TestData.Now, maxAttempts: 3);

        user.Status.Should().Be(UserStatus.Locked);
        user.LockoutEndUtc.Should().NotBeNull();
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserLocked);
    }
}
