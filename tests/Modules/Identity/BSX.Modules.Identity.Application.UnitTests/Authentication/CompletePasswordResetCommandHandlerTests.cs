using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication.CompletePasswordReset;
using BSX.Modules.Identity.Application.Authentication.RequestPasswordReset;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.Users.Credentials;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Authentication;

public sealed class CompletePasswordResetCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryUserSessionRepository _sessions = new();
    private readonly FakePasswordResetNotifier _notifier = new();
    private readonly FakePasswordResetTokenHasher _tokenHasher = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MutableTimeProvider _timeProvider = new(TestData.Now);

    private RequestPasswordResetCommandHandler CreateRequestHandler() => new(
        _users,
        _tokenHasher,
        new FakeSecureTokenGenerator(),
        _notifier,
        new FakeRateLimiter(),
        new RateLimitOptions(),
        _unitOfWork,
        _timeProvider);

    private CompletePasswordResetCommandHandler CreateCompleteHandler() => new(
        _users,
        _sessions,
        _tokenHasher,
        _passwordHasher,
        _unitOfWork,
        _timeProvider);

    private async Task<(Guid UserId, string Token)> RequestAsync()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;
        user.SetPassword(TestData.CreatePasswordHash("OldPassw0rd!"), TestData.Now);
        user.Activate(TestData.Now);
        _users.Seed(user);

        await CreateRequestHandler().HandleAsync(new RequestPasswordResetCommand("user@example.com"));
        return (user.Id.Value, _notifier.Notifications[0].Token);
    }

    [Fact]
    public async Task Should_ResetPassword_WhenTokenValid()
    {
        (Guid userId, string token) = await RequestAsync();

        var result = await CreateCompleteHandler().HandleAsync(new CompletePasswordResetCommand(token, "NewPassw0rd!"));

        result.IsSuccess.Should().BeTrue();
        User user = _users.Users.Single(candidate => candidate.Id.Value == userId);
        PasswordCredential credential = user.Credentials.OfType<PasswordCredential>().Single();
        _passwordHasher.Verify("NewPassw0rd!", credential.Hash).Should().BeTrue();
        user.PasswordResetToken!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_WhenTokenUnknown()
    {
        await RequestAsync();

        var result = await CreateCompleteHandler().HandleAsync(new CompletePasswordResetCommand("wrong-token", "NewPassw0rd!"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.PasswordResetTokenInvalid");
    }

    [Fact]
    public async Task Should_Fail_WhenTokenReused()
    {
        (_, string token) = await RequestAsync();
        CompletePasswordResetCommandHandler handler = CreateCompleteHandler();
        await handler.HandleAsync(new CompletePasswordResetCommand(token, "NewPassw0rd!"));

        var replay = await handler.HandleAsync(new CompletePasswordResetCommand(token, "AnotherPassw0rd!"));

        replay.IsFailure.Should().BeTrue();
        replay.Error.Code.Should().Be("Identity.User.PasswordResetTokenUsed");
    }

    [Fact]
    public async Task Should_Fail_WhenTokenExpired()
    {
        (_, string token) = await RequestAsync();

        _timeProvider.UtcNow = TestData.Now.AddMinutes(31);

        var result = await CreateCompleteHandler().HandleAsync(new CompletePasswordResetCommand(token, "NewPassw0rd!"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.User.PasswordResetTokenExpired");
    }

    [Fact]
    public async Task Should_RevokeSessions_WhenResetCompletes()
    {
        (Guid userId, string token) = await RequestAsync();
        User tracked = _users.Users.Single(candidate => candidate.Id.Value == userId);

        var session = UserSession.Start(
            SessionId.New(),
            tracked.Id,
            null,
            tracked.SecurityStamp.Value,
            RefreshTokenHash.From("rt"),
            SessionMetadata.Create("ua", "ip"),
            TestData.Now,
            SessionPolicy.Default).Value;
        await _sessions.AddAsync(session);

        await CreateCompleteHandler().HandleAsync(new CompletePasswordResetCommand(token, "NewPassw0rd!"));

        session.Status.Should().Be(SessionStatus.Revoked);
    }
}
