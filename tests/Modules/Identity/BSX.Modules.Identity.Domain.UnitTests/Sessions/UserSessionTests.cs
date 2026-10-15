using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.UnitTests.Sessions;

public sealed class UserSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly SessionPolicy Policy = new(
        TokenLifetime: TimeSpan.FromMinutes(5),
        IdleTimeout: TimeSpan.FromHours(1),
        AbsoluteLifetime: TimeSpan.FromDays(1),
        GraceWindow: TimeSpan.FromSeconds(30));

    private static UserSession Start() => UserSession.Start(
        SessionId.New(),
        UserId.New(),
        null,
        "stamp",
        RefreshTokenHash.From("h1"),
        SessionMetadata.Create("ua", "ip"),
        Now,
        Policy).Value;

    [Fact]
    public void Should_StartActive_WithOneToken()
    {
        UserSession session = Start();

        session.IsActive(Now).Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Active);
        session.RefreshTokens.Should().ContainSingle();
        session.CurrentToken!.Hash.Should().Be(RefreshTokenHash.From("h1"));
    }

    [Fact]
    public void Should_Rotate_WhenActiveTokenPresented()
    {
        UserSession session = Start();

        Result result = session.Rotate(RefreshTokenHash.From("h1"), RefreshTokenHash.From("h2"), Now.AddSeconds(1), Policy);

        result.IsSuccess.Should().BeTrue();
        session.CurrentToken!.Hash.Should().Be(RefreshTokenHash.From("h2"));
    }

    [Fact]
    public void Should_AllowGraceWindow()
    {
        UserSession session = Start();
        session.Rotate(RefreshTokenHash.From("h1"), RefreshTokenHash.From("h2"), Now.AddSeconds(1), Policy);

        Result result = session.Rotate(RefreshTokenHash.From("h1"), RefreshTokenHash.From("h3"), Now.AddSeconds(10), Policy);

        result.IsSuccess.Should().BeTrue();
        session.CurrentToken!.Hash.Should().Be(RefreshTokenHash.From("h3"));
    }

    [Fact]
    public void Should_DetectReuse_BeyondGrace()
    {
        UserSession session = Start();
        session.Rotate(RefreshTokenHash.From("h1"), RefreshTokenHash.From("h2"), Now.AddSeconds(1), Policy);

        Result result = session.Rotate(RefreshTokenHash.From("h1"), RefreshTokenHash.From("h3"), Now.AddMinutes(5), Policy);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Session.RefreshTokenReuse");
        session.Status.Should().Be(SessionStatus.Revoked);
        session.IsActive(Now.AddMinutes(5)).Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_WhenTokenUnknown()
    {
        UserSession session = Start();

        Result result = session.Rotate(RefreshTokenHash.From("unknown"), RefreshTokenHash.From("h2"), Now, Policy);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Session.RefreshTokenInvalid");
    }

    [Fact]
    public void Should_Fail_WhenTokenExpired()
    {
        UserSession session = Start();

        Result result = session.Rotate(RefreshTokenHash.From("h1"), RefreshTokenHash.From("h2"), Now.AddMinutes(6), Policy);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Session.RefreshTokenExpired");
    }

    [Fact]
    public void Should_Fail_WhenSessionExpired()
    {
        UserSession session = UserSession.Start(
            SessionId.New(),
            UserId.New(),
            null,
            "stamp",
            RefreshTokenHash.From("h1"),
            SessionMetadata.Create(null, null),
            Now,
            new SessionPolicy(TimeSpan.FromDays(1), TimeSpan.FromMinutes(1), TimeSpan.FromDays(1), TimeSpan.FromSeconds(30))).Value;

        Result result = session.Rotate(RefreshTokenHash.From("h1"), RefreshTokenHash.From("h2"), Now.AddMinutes(2), Policy);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Session.Expired");
    }

    [Fact]
    public void Should_Revoke()
    {
        UserSession session = Start();

        session.Revoke(Now.AddMinutes(1), "manual").IsSuccess.Should().BeTrue();

        session.Status.Should().Be(SessionStatus.Revoked);
        session.RevokedReason.Should().Be("manual");
        session.IsActive(Now.AddMinutes(1)).Should().BeFalse();
    }
}
