using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.Users;

public sealed class UserPasswordResetTests
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    [Fact]
    public void Should_RequestReset_AndRaiseEvent()
    {
        User user = TestData.CreateActiveUser();
        var tokenId = PasswordResetTokenId.New();

        Result result = user.RequestPasswordReset(tokenId, PasswordResetTokenHash.From("hash-1"), TestData.Now, Lifetime);

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetToken.Should().NotBeNull();
        user.PasswordResetToken!.IsUsed.Should().BeFalse();
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserPasswordResetRequested);
    }

    [Fact]
    public void Should_Fail_WhenTokenHashDoesNotMatch()
    {
        User user = TestData.CreateActiveUser();
        user.RequestPasswordReset(PasswordResetTokenId.New(), PasswordResetTokenHash.From("hash-1"), TestData.Now, Lifetime);

        Result result = user.CompletePasswordReset(PasswordResetTokenHash.From("wrong"), TestData.CreatePasswordHash("new"), TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.PasswordResetTokenInvalid");
    }

    [Fact]
    public void Should_Fail_WhenTokenExpired()
    {
        User user = TestData.CreateActiveUser();
        user.RequestPasswordReset(PasswordResetTokenId.New(), PasswordResetTokenHash.From("hash-1"), TestData.Now, Lifetime);

        Result result = user.CompletePasswordReset(
            PasswordResetTokenHash.From("hash-1"),
            TestData.CreatePasswordHash("new"),
            TestData.Now.AddMinutes(31));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.PasswordResetTokenExpired");
    }

    [Fact]
    public void Should_CompleteReset_AndRejectReuse()
    {
        User user = TestData.CreateActiveUser();
        user.RequestPasswordReset(PasswordResetTokenId.New(), PasswordResetTokenHash.From("hash-1"), TestData.Now, Lifetime);

        Result result = user.CompletePasswordReset(
            PasswordResetTokenHash.From("hash-1"),
            TestData.CreatePasswordHash("new"),
            TestData.Now.AddMinutes(1));

        result.IsSuccess.Should().BeTrue();
        user.PasswordResetToken!.IsUsed.Should().BeTrue();
        user.DomainEvents.Should().Contain(domainEvent => domainEvent is UserPasswordResetCompleted);

        Result reuse = user.CompletePasswordReset(
            PasswordResetTokenHash.From("hash-1"),
            TestData.CreatePasswordHash("again"),
            TestData.Now.AddMinutes(2));

        reuse.IsFailure.Should().BeTrue();
        reuse.Error.Code.Should().Be("Identity.User.PasswordResetTokenUsed");
    }

    [Fact]
    public void Should_InvalidatePreviousToken_WhenResetRequestedAgain()
    {
        User user = TestData.CreateActiveUser();
        user.RequestPasswordReset(PasswordResetTokenId.New(), PasswordResetTokenHash.From("hash-1"), TestData.Now, Lifetime);

        user.RequestPasswordReset(PasswordResetTokenId.New(), PasswordResetTokenHash.From("hash-2"), TestData.Now, Lifetime);

        Result result = user.CompletePasswordReset(
            PasswordResetTokenHash.From("hash-1"),
            TestData.CreatePasswordHash("new"),
            TestData.Now.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.PasswordResetTokenInvalid");
    }
}
