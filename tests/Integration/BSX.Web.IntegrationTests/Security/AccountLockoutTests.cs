using System.Security.Claims;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Authentication.DiscoverTenantOptions;
using BSX.Modules.Identity.Application.Authentication.Login;
using BSX.Modules.Identity.Application.Authentication.RefreshToken;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Users.ActivateUser;
using BSX.Modules.Identity.Application.Users.DeactivateUser;
using BSX.Modules.Identity.Application.Users.RegisterUser;
using BSX.Modules.Identity.Application.Users.UnlockUser;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.SharedKernel.Results;
using BSX.Web.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Web.IntegrationTests.Security;

/// <summary>
/// Adversarial coverage for durable account lockout (ADR-022). The login command transaction rolls
/// back failed results, so these tests prove the failure counter, lockout, session revocation and
/// refresh rejection survive that rollback.
/// </summary>
public sealed class AccountLockoutTests : IClassFixture<IdentityApiFactory>
{
    private const string WrongPassword = "DefinitelyWrong!";

    private readonly IdentityApiFactory _factory;

    public AccountLockoutTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task FailedLogin_ShouldPersistFailureCount_AfterCommandRollback()
    {
        LockoutUser user = await CreateUserAsync();

        Result<AuthenticationTokensResponse> result = await SendAsync(
            new LoginCommand(user.Email, WrongPassword, null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidCredentials");

        User persisted = await QueryUserAsync(user.UserId);
        persisted.AccessFailedCount.Should().Be(1);
        persisted.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task TenantDiscoveryFailure_ShouldAlsoPersistFailureCount()
    {
        LockoutUser user = await CreateUserAsync();

        Result<IReadOnlyList<TenantOptionDto>> result = await SendAsync(
            new DiscoverTenantOptionsCommand(user.Email, WrongPassword));

        result.IsFailure.Should().BeTrue();

        User persisted = await QueryUserAsync(user.UserId);
        persisted.AccessFailedCount.Should().Be(1);
    }

    [Fact]
    public async Task ThresholdReached_ShouldPersistLockout_AndRevokeSessions()
    {
        LockoutUser user = await CreateUserAsync();

        Result<AuthenticationTokensResponse> login = await SendAsync(
            new LoginCommand(user.Email, IdentityApiFactory.AdminPassword, null, null, null));
        login.IsSuccess.Should().BeTrue();

        Guid sessionId = await GetLatestSessionIdAsync(user.UserId);
        string originalStamp = (await QueryUserAsync(user.UserId)).SecurityStamp.Value;

        await FailLoginAsync(user, attempts: 3);

        User locked = await QueryUserAsync(user.UserId);
        locked.Status.Should().Be(UserStatus.Locked);
        locked.AccessFailedCount.Should().Be(3);
        locked.LockoutEndUtc.Should().NotBeNull();
        locked.SecurityStamp.Value.Should().NotBe(originalStamp);

        UserSession session = await QuerySessionAsync(sessionId);
        session.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task LockedLogin_ShouldBeRejected_WithoutExtendingLockout()
    {
        LockoutUser user = await CreateUserAsync();
        await FailLoginAsync(user, attempts: 3);

        User before = await QueryUserAsync(user.UserId);
        DateTimeOffset? lockoutEnd = before.LockoutEndUtc;
        int failureCount = before.AccessFailedCount;

        Result<AuthenticationTokensResponse> result = await SendAsync(
            new LoginCommand(user.Email, IdentityApiFactory.AdminPassword, null, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Identity.Authentication.InvalidCredentials");

        User after = await QueryUserAsync(user.UserId);
        after.LockoutEndUtc.Should().Be(lockoutEnd);
        after.AccessFailedCount.Should().Be(failureCount);
    }

    [Fact]
    public async Task LockedRefresh_ShouldBeRejected_AndSessionRevoked()
    {
        LockoutUser user = await CreateUserAsync();

        Result<AuthenticationTokensResponse> login = await SendAsync(
            new LoginCommand(user.Email, IdentityApiFactory.AdminPassword, null, null, null));
        login.IsSuccess.Should().BeTrue();

        Guid sessionId = await GetLatestSessionIdAsync(user.UserId);
        await FailLoginAsync(user, attempts: 3);

        Result<AuthenticationTokensResponse> refresh = await SendAsync(
            new RefreshTokenCommand(login.Value.RefreshToken));

        refresh.IsFailure.Should().BeTrue();
        refresh.Error.Code.Should().Be("Identity.Authentication.InvalidRefreshToken");

        UserSession session = await QuerySessionAsync(sessionId);
        session.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task DisabledRefresh_ShouldBeRejected_AndSessionRevoked()
    {
        LockoutUser user = await CreateUserAsync();

        Result<AuthenticationTokensResponse> login = await SendAsync(
            new LoginCommand(user.Email, IdentityApiFactory.AdminPassword, null, null, null));
        login.IsSuccess.Should().BeTrue();

        Guid sessionId = await GetLatestSessionIdAsync(user.UserId);
        (await SendAsAdminAsync(new DeactivateUserCommand(user.UserId))).IsSuccess.Should().BeTrue();

        Result<AuthenticationTokensResponse> refresh = await SendAsync(
            new RefreshTokenCommand(login.Value.RefreshToken));

        refresh.IsFailure.Should().BeTrue();
        refresh.Error.Code.Should().Be("Identity.Authentication.InvalidRefreshToken");

        UserSession session = await QuerySessionAsync(sessionId);
        session.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task AdminUnlock_ShouldClearLockout_AndRequireNewSignIn()
    {
        LockoutUser user = await CreateUserAsync();

        Result<AuthenticationTokensResponse> login = await SendAsync(
            new LoginCommand(user.Email, IdentityApiFactory.AdminPassword, null, null, null));
        login.IsSuccess.Should().BeTrue();

        await FailLoginAsync(user, attempts: 3);

        Result unlock = await SendAsAdminAsync(new UnlockUserCommand(user.UserId));
        unlock.IsSuccess.Should().BeTrue();

        User unlocked = await QueryUserAsync(user.UserId);
        unlocked.Status.Should().Be(UserStatus.Active);
        unlocked.AccessFailedCount.Should().Be(0);
        unlocked.LockoutEndUtc.Should().BeNull();

        // The pre-lockout refresh token must not be revived by the unlock.
        Result<AuthenticationTokensResponse> refresh = await SendAsync(
            new RefreshTokenCommand(login.Value.RefreshToken));
        refresh.IsFailure.Should().BeTrue();

        Result<AuthenticationTokensResponse> freshLogin = await SendAsync(
            new LoginCommand(user.Email, IdentityApiFactory.AdminPassword, null, null, null));
        freshLogin.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task LockoutExpiry_ShouldAllowSignIn()
    {
        LockoutUser user = await CreateUserAsync();
        await FailLoginAsync(user, attempts: 3);

        DateTimeOffset original = _factory.Clock.GetUtcNow();

        try
        {
            _factory.Clock.Advance(TimeSpan.FromMinutes(2));

            Result<AuthenticationTokensResponse> result = await SendAsync(
                new LoginCommand(user.Email, IdentityApiFactory.AdminPassword, null, null, null));

            result.IsSuccess.Should().BeTrue();
        }
        finally
        {
            _factory.Clock.Set(original);
        }

        User unlocked = await QueryUserAsync(user.UserId);
        unlocked.Status.Should().Be(UserStatus.Active);
        unlocked.AccessFailedCount.Should().Be(0);
        unlocked.LockoutEndUtc.Should().BeNull();
    }

    private async Task FailLoginAsync(LockoutUser user, int attempts)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Result<AuthenticationTokensResponse> result = await SendAsync(
                new LoginCommand(user.Email, WrongPassword, null, null, null));

            result.IsFailure.Should().BeTrue();
        }
    }

    private async Task<LockoutUser> CreateUserAsync()
    {
        string email = $"lockout-{Guid.NewGuid():N}@example.com";

        Result<RegisterUserResponse> registered = await SendAsAdminAsync(
            new RegisterUserCommand(email, "Lockout User", IdentityApiFactory.AdminPassword));
        registered.IsSuccess.Should().BeTrue();

        Guid userId = registered.Value.UserId;
        (await SendAsAdminAsync(new ActivateUserCommand(userId))).IsSuccess.Should().BeTrue();

        return new LockoutUser(userId, email);
    }

    private async Task<Guid> GetLatestSessionIdAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        UserSession session = await context.Sessions
            .AsNoTracking()
            .SingleAsync(candidate => candidate.UserId == UserId.From(userId) && candidate.Status == SessionStatus.Active);
        return session.Id.Value;
    }

    private async Task<User> QueryUserAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await context.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == UserId.From(userId));
    }

    private async Task<UserSession> QuerySessionAsync(Guid sessionId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await context.Sessions
            .AsNoTracking()
            .SingleAsync(session => session.Id == SessionId.From(sessionId));
    }

    private async Task<T> SendAsync<T>(IRequest<T> request)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(request);
    }

    private async Task<T> SendAsAdminAsync<T>(IRequest<T> request)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<WebCurrentUser>().SetPrincipal(CreateAdminPrincipal());
        return await scope.ServiceProvider.GetRequiredService<ISender>().SendAsync(request);
    }

    private ClaimsPrincipal CreateAdminPrincipal()
    {
        var claims = new List<Claim>
        {
            new(WebAuthDefaults.UserIdClaim, _factory.AdminUserId.ToString()),
            new(WebAuthDefaults.EmailClaim, IdentityApiFactory.AdminEmail),
            new(WebAuthDefaults.NameClaim, "Administrator"),
            new(WebAuthDefaults.TenantIdClaim, _factory.TenantId.ToString()),
        };

        claims.AddRange(IdentityApiFactory.AdminPermissions.Select(
            permission => new Claim(WebAuthDefaults.PermissionClaim, permission)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }

    private sealed record LockoutUser(Guid UserId, string Email);
}
