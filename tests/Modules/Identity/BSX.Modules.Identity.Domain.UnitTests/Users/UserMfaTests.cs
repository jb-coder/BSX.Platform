using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.Users;

public sealed class UserMfaTests
{
    private static readonly RecoveryCodeHash[] RecoveryCodes =
        [RecoveryCodeHash.From("code-1"), RecoveryCodeHash.From("code-2")];

    [Fact]
    public void Should_EnableMfa_AndRaiseEvent()
    {
        User user = TestData.CreateActiveUser();

        Result result = user.EnableMfa(TotpSecret.From("secret"), RecoveryCodes, TestData.Now);

        result.IsSuccess.Should().BeTrue();
        user.IsMfaEnabled.Should().BeTrue();
        user.HasCredential(CredentialType.Totp).Should().BeTrue();
        user.HasCredential(CredentialType.RecoveryCodes).Should().BeTrue();
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserMfaEnabled);
    }

    [Fact]
    public void Should_FailEnableMfa_WhenAlreadyEnabled()
    {
        User user = TestData.CreateActiveUser();
        user.EnableMfa(TotpSecret.From("secret"), RecoveryCodes, TestData.Now);

        Result result = user.EnableMfa(TotpSecret.From("secret2"), RecoveryCodes, TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.MfaAlreadyEnabled");
    }

    [Fact]
    public void Should_DisableMfa_AndRemoveCredentials()
    {
        User user = TestData.CreateActiveUser();
        user.EnableMfa(TotpSecret.From("secret"), RecoveryCodes, TestData.Now);

        Result result = user.DisableMfa();

        result.IsSuccess.Should().BeTrue();
        user.IsMfaEnabled.Should().BeFalse();
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserMfaDisabled);
    }

    [Fact]
    public void Should_ConsumeRecoveryCode_Once()
    {
        User user = TestData.CreateActiveUser();
        user.EnableMfa(TotpSecret.From("secret"), RecoveryCodes, TestData.Now);

        user.ConsumeRecoveryCode(RecoveryCodeHash.From("code-1")).IsSuccess.Should().BeTrue();

        Result reuse = user.ConsumeRecoveryCode(RecoveryCodeHash.From("code-1"));
        reuse.IsFailure.Should().BeTrue();
        reuse.Error.Code.Should().Be("Identity.User.RecoveryCodeUsed");
    }

    [Fact]
    public void Should_Fail_WhenRecoveryCodeUnknown()
    {
        User user = TestData.CreateActiveUser();
        user.EnableMfa(TotpSecret.From("secret"), RecoveryCodes, TestData.Now);

        Result result = user.ConsumeRecoveryCode(RecoveryCodeHash.From("unknown"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.RecoveryCodeInvalid");
    }

    [Fact]
    public void Should_Fail_WhenMfaNotEnabled()
    {
        User user = TestData.CreateActiveUser();

        Result result = user.ConsumeRecoveryCode(RecoveryCodeHash.From("code-1"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.MfaNotEnabled");
    }
}
