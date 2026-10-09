using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.Users;

public sealed class UserRegistrationTests
{
    [Fact]
    public void Should_Register_AsPending_AndRaiseEvent()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;

        user.Status.Should().Be(UserStatus.Pending);
        user.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is UserRegistered);
    }

    [Fact]
    public void Should_FailActivation_WhenNoCredential()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;

        Result result = user.Activate(TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.CredentialRequired");
        user.Status.Should().Be(UserStatus.Pending);
    }

    [Fact]
    public void Should_Activate_WhenCredentialExists()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;
        user.SetPassword(TestData.CreatePasswordHash(), TestData.Now);

        Result result = user.Activate(TestData.Now);

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserActivated);
    }

    [Fact]
    public void Should_RotateSecurityStamp_OnActivation()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;
        user.SetPassword(TestData.CreatePasswordHash(), TestData.Now);
        var before = user.SecurityStamp;

        user.Activate(TestData.Now);

        user.SecurityStamp.Should().NotBe(before);
    }
}
