using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Authentication.Login;
using BSX.Modules.Identity.Application.Authentication.RefreshToken;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Authentication;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryUserSessionRepository _sessions = new();
    private readonly InMemoryTenantMembershipRepository _memberships = new();
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeAccountSecurityStore _securityStore = new();
    private readonly FakePostCommandActionQueue _postCommandActions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MutableTimeProvider _timeProvider = new(TestData.Now);

    private LoginCommandHandler CreateLoginHandler() => new(
        _sessions,
        _memberships,
        new PermissionResolver(_roles, TestPermissionCatalog.Platform),
        new CredentialAuthenticator(_users, _passwordHasher, _securityStore, _postCommandActions, _timeProvider),
        new FakeSecureTokenGenerator(),
        new FakeRefreshTokenHasher(),
        new FakeAccessTokenIssuer(),
        _unitOfWork,
        _timeProvider,
        new FakeRateLimiter(),
        new RateLimitOptions());

    private RefreshTokenCommandHandler CreateRefreshHandler(IRateLimiter? rateLimiter = null) => new(
        _users,
        _sessions,
        _memberships,
        new PermissionResolver(_roles, TestPermissionCatalog.Platform),
        new FakeRefreshTokenHasher(),
        new FakeSecureTokenGenerator(),
        new FakeAccessTokenIssuer(),
        _securityStore,
        _postCommandActions,
        _unitOfWork,
        _timeProvider,
        rateLimiter ?? new FakeRateLimiter(),
        new RateLimitOptions());

    private async Task<string> LoginAsync()
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;
        user.SetPassword(_passwordHasher.Hash("Passw0rd!"), TestData.Now);
        user.Activate(TestData.Now);
        _users.Seed(user);

        var login = await CreateLoginHandler().HandleAsync(new LoginCommand("user@example.com", "Passw0rd!", null, null, null));
        return login.Value.RefreshToken;
    }

    [Fact]
    public async Task Should_Rotate_RefreshToken()
    {
        string refreshToken = await LoginAsync();

        var result = await CreateRefreshHandler().HandleAsync(new RefreshTokenCommand(refreshToken));

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().NotBe(refreshToken);
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_DetectReuse_BeyondGrace_AndPersistRevocation()
    {
        string refreshToken = await LoginAsync();
        Guid sessionId = _sessions.Sessions.Single().Id.Value;

        var first = await CreateRefreshHandler().HandleAsync(new RefreshTokenCommand(refreshToken));
        first.IsSuccess.Should().BeTrue();

        _timeProvider.UtcNow = TestData.Now.AddSeconds(60);

        var replay = await CreateRefreshHandler().HandleAsync(new RefreshTokenCommand(refreshToken));

        replay.IsFailure.Should().BeTrue();
        replay.Error.Code.Should().Be("Identity.Session.RefreshTokenReuse");

        await _postCommandActions.FlushAsync();
        _securityStore.Revocations.Should().ContainSingle(revocation =>
            revocation.SessionId.Value == sessionId
            && revocation.Reason == "refresh_token_reuse");
    }

    [Fact]
    public async Task Should_Reject_Refresh_WhenUserLocked_AndPersistRevocation()
    {
        string refreshToken = await LoginAsync();
        User user = _users.Users.Single();
        user.Lock(TestData.Now.AddMinutes(15), TestData.Now).IsSuccess.Should().BeTrue();

        var result = await CreateRefreshHandler().HandleAsync(new RefreshTokenCommand(refreshToken));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidRefreshToken");

        await _postCommandActions.FlushAsync();
        _securityStore.Revocations.Should().ContainSingle(revocation =>
            revocation.Reason == "account_not_active");
    }

    [Fact]
    public async Task Should_Reject_Refresh_WhenUserDisabled_AndPersistRevocation()
    {
        string refreshToken = await LoginAsync();
        User user = _users.Users.Single();
        user.Deactivate().IsSuccess.Should().BeTrue();

        var result = await CreateRefreshHandler().HandleAsync(new RefreshTokenCommand(refreshToken));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidRefreshToken");

        await _postCommandActions.FlushAsync();
        _securityStore.Revocations.Should().ContainSingle(revocation =>
            revocation.Reason == "account_not_active");
    }

    [Fact]
    public async Task Should_RevokeSession_WhenSecurityStampChanged()
    {
        string refreshToken = await LoginAsync();
        User user = _users.Users.Single();
        user.ChangeEmail(TestData.CreateEmail("changed@example.com")).IsSuccess.Should().BeTrue();

        var result = await CreateRefreshHandler().HandleAsync(new RefreshTokenCommand(refreshToken));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidRefreshToken");

        await _postCommandActions.FlushAsync();
        _securityStore.Revocations.Should().ContainSingle(revocation =>
            revocation.Reason == "security_stamp_changed");
    }

    [Fact]
    public async Task Should_RateLimit_Refresh()
    {
        string refreshToken = await LoginAsync();
        RefreshTokenCommandHandler handler = CreateRefreshHandler(new FakeRateLimiter(maxRequests: 1));

        var first = await handler.HandleAsync(new RefreshTokenCommand(refreshToken));
        var second = await handler.HandleAsync(new RefreshTokenCommand(refreshToken));

        first.IsSuccess.Should().BeTrue();
        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("Identity.Authentication.TooManyRequests");
    }
}
