using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.Users.Credentials;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.Users;

public sealed class UserCredentialTests
{
    [Fact]
    public void Should_FailChangePassword_WhenNoPasswordCredential()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;

        Result result = user.ChangePassword(TestData.CreatePasswordHash("new"), TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.NoPasswordCredential");
    }

    [Fact]
    public void Should_RotateSecurityStamp_OnPasswordChange()
    {
        User user = TestData.CreateActiveUser();
        var before = user.SecurityStamp;

        user.ChangePassword(TestData.CreatePasswordHash("new"), TestData.Now).IsSuccess.Should().BeTrue();

        user.SecurityStamp.Should().NotBe(before);
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserPasswordChanged);
    }

    [Fact]
    public void Should_Fail_WhenExternalProviderAlreadyLinked()
    {
        User user = TestData.CreateActiveUser();
        user.AddExternalCredential(ExternalProvider.Google, "subject-1", TestData.Now).IsSuccess.Should().BeTrue();

        Result result = user.AddExternalCredential(ExternalProvider.Google, "subject-1", TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.ExternalProviderAlreadyLinked");
    }

    [Fact]
    public void Should_FailRemoving_LastCredential_WhenActive()
    {
        User user = TestData.CreateActiveUser();
        Credential credential = user.Credentials.Single();

        Result result = user.RemoveCredential(credential.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.LastCredential");
    }

    [Fact]
    public void Should_RemoveCredential_WhenOthersRemain()
    {
        User user = TestData.CreateActiveUser();
        user.AddPasskey("passkey-1", new byte[] { 1, 2, 3 }, "laptop", TestData.Now);
        Credential passkey = user.Credentials.Single(credential => credential.Type == CredentialType.WebAuthn);

        Result result = user.RemoveCredential(passkey.Id);

        result.IsSuccess.Should().BeTrue();
        user.Credentials.Should().NotContain(credential => credential.Type == CredentialType.WebAuthn);
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserCredentialRemoved);
    }
}
