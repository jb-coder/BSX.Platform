using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Authentication.Login;
using BSX.Modules.Identity.Application.UnitTests.Fakes;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Authentication;

public sealed class LoginCommandHandlerTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryUserSessionRepository _sessions = new();
    private readonly InMemoryTenantMembershipRepository _memberships = new();
    private readonly InMemoryRoleRepository _roles = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MutableTimeProvider _timeProvider = new(TestData.Now);

    private LoginCommandHandler CreateHandler(IRateLimiter? rateLimiter = null) => new(
        _users,
        _sessions,
        _memberships,
        new PermissionResolver(_roles),
        _passwordHasher,
        new FakeSecureTokenGenerator(),
        new FakeRefreshTokenHasher(),
        new FakeAccessTokenIssuer(),
        _unitOfWork,
        _timeProvider,
        rateLimiter ?? new FakeRateLimiter(),
        new RateLimitOptions());

    private User SeedUser(bool activate)
    {
        User user = User.Register(UserId.New(), TestData.CreateEmail(), TestData.CreateName(), TestData.Now).Value;
        user.SetPassword(_passwordHasher.Hash("Passw0rd!"), TestData.Now);

        if (activate)
        {
            user.Activate(TestData.Now);
        }

        _users.Seed(user);
        return user;
    }

    [Fact]
    public async Task Should_Login_WhenCredentialsValid()
    {
        SeedUser(activate: true);

        var result = await CreateHandler().HandleAsync(new LoginCommand("user@example.com", "Passw0rd!", null, "ua", "127.0.0.1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        _sessions.Sessions.Should().ContainSingle();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenPasswordInvalid()
    {
        SeedUser(activate: true);

        var result = await CreateHandler().HandleAsync(new LoginCommand("user@example.com", "wrong", null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidCredentials");
        _sessions.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_WhenUserInactive()
    {
        SeedUser(activate: false);

        var result = await CreateHandler().HandleAsync(new LoginCommand("user@example.com", "Passw0rd!", null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidCredentials");
        _users.Users.Should().OnlyContain(user => user.Status == UserStatus.Pending);
    }

    [Fact]
    public async Task Should_RateLimit_AfterThreshold()
    {
        SeedUser(activate: true);
        LoginCommandHandler handler = CreateHandler(new FakeRateLimiter(maxRequests: 2));

        await handler.HandleAsync(new LoginCommand("user@example.com", "Passw0rd!", null, null, null));
        await handler.HandleAsync(new LoginCommand("user@example.com", "Passw0rd!", null, null, null));
        var limited = await handler.HandleAsync(new LoginCommand("user@example.com", "Passw0rd!", null, null, null));

        limited.IsFailure.Should().BeTrue();
        limited.Error.Code.Should().Be("Identity.Authentication.TooManyRequests");
    }
}
