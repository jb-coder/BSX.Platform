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
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MutableTimeProvider _timeProvider = new(TestData.Now);

    private LoginCommandHandler CreateLoginHandler() => new(
        _sessions,
        _memberships,
        new PermissionResolver(_roles),
        new CredentialAuthenticator(_users, _passwordHasher, _unitOfWork, _timeProvider),
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
        new PermissionResolver(_roles),
        new FakeRefreshTokenHasher(),
        new FakeSecureTokenGenerator(),
        new FakeAccessTokenIssuer(),
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
    public async Task Should_DetectReuse_BeyondGrace()
    {
        string refreshToken = await LoginAsync();

        var first = await CreateRefreshHandler().HandleAsync(new RefreshTokenCommand(refreshToken));
        first.IsSuccess.Should().BeTrue();

        _timeProvider.UtcNow = TestData.Now.AddSeconds(60);

        var replay = await CreateRefreshHandler().HandleAsync(new RefreshTokenCommand(refreshToken));

        replay.IsFailure.Should().BeTrue();
        replay.Error.Code.Should().Be("Identity.Session.RefreshTokenReuse");
        _sessions.Sessions.Should().OnlyContain(session => session.Status == Domain.Sessions.SessionStatus.Revoked);
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
