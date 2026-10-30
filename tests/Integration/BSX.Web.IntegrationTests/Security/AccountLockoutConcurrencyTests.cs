using System.Security.Claims;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Application.Authentication.Login;
using BSX.Modules.Identity.Application.Authentication.RefreshToken;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Users.ActivateUser;
using BSX.Modules.Identity.Application.Users.RegisterUser;
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
/// Adversarial concurrency coverage for ADR-022. A concurrent session mutation must never roll back
/// a durable account lockout: the user security state is committed before session cleanup.
/// </summary>
public sealed class AccountLockoutConcurrencyTests : IClassFixture<IdentityApiFactory>
{
    private const string WrongPassword = "DefinitelyWrong!";

    private readonly IdentityApiFactory _factory;

    public AccountLockoutConcurrencyTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ConcurrentSessionRevoke_ShouldNotRollBackLockout()
    {
        GetInterceptor().Clear();
        LockoutUser user = await CreateUserAsync();
        (await SignInAsync(user)).IsSuccess.Should().BeTrue();

        Guid sessionId = await GetLatestSessionIdAsync(user.UserId);
        await FailDirectlyAsync(user, attempts: 2);

        GetInterceptor().ForceSessionConflict(
            sessionId,
            ForcedIdentityConcurrencySaveChangesInterceptor.SessionConflictMode.Revoke);

        await RecordFailureAsync(user.UserId);

        User locked = await QueryUserAsync(user.UserId);
        locked.Status.Should().Be(UserStatus.Locked);
        locked.AccessFailedCount.Should().Be(3);
        locked.LockoutEndUtc.Should().NotBeNull();

        UserSession session = await QuerySessionAsync(sessionId);
        session.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task ConcurrentSessionUpdate_ShouldNotRollBackLockout()
    {
        GetInterceptor().Clear();
        LockoutUser user = await CreateUserAsync();
        (await SignInAsync(user)).IsSuccess.Should().BeTrue();

        Guid sessionId = await GetLatestSessionIdAsync(user.UserId);
        await FailDirectlyAsync(user, attempts: 2);

        GetInterceptor().ForceSessionConflict(
            sessionId,
            ForcedIdentityConcurrencySaveChangesInterceptor.SessionConflictMode.Update);

        await RecordFailureAsync(user.UserId);

        User locked = await QueryUserAsync(user.UserId);
        locked.Status.Should().Be(UserStatus.Locked);
        locked.AccessFailedCount.Should().Be(3);

        UserSession session = await QuerySessionAsync(sessionId);
        session.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task SecurityStampRotationConflict_ShouldStillPersistLockout()
    {
        GetInterceptor().Clear();
        LockoutUser user = await CreateUserAsync();
        await FailDirectlyAsync(user, attempts: 2);

        string preLockoutStamp = (await QueryUserAsync(user.UserId)).SecurityStamp.Value;

        GetInterceptor().ForceUserConflict(user.UserId);

        await RecordFailureAsync(user.UserId);

        User locked = await QueryUserAsync(user.UserId);
        locked.Status.Should().Be(UserStatus.Locked);
        locked.AccessFailedCount.Should().Be(3);
        locked.LockoutEndUtc.Should().NotBeNull();
        locked.SecurityStamp.Value.Should().NotBe(preLockoutStamp);
    }

    [Fact]
    public async Task SessionsRemainingActiveTransiently_ShouldStillBeRejectedByRevalidation()
    {
        GetInterceptor().Clear();
        LockoutUser user = await CreateUserAsync();
        Result<AuthenticationTokensResponse> login = await SignInAsync(user);
        login.IsSuccess.Should().BeTrue();

        Guid sessionId = await GetLatestSessionIdAsync(user.UserId);
        await FailDirectlyAsync(user, attempts: 2);

        // Session cleanup can never succeed, simulating a transiently surviving session.
        GetInterceptor().ForceSessionConflict(
            sessionId,
            ForcedIdentityConcurrencySaveChangesInterceptor.SessionConflictMode.Revoke,
            times: int.MaxValue);

        await RecordFailureAsync(user.UserId);

        User locked = await QueryUserAsync(user.UserId);
        locked.Status.Should().Be(UserStatus.Locked);

        UserSession session = await QuerySessionAsync(sessionId);
        session.Status.Should().Be(SessionStatus.Active);

        // Authorization reads the authoritative store, so the stale session is denied even though
        // the row is still marked active.
        AuthorizationSnapshot? snapshot = await ReadSnapshotAsync(user.UserId, sessionId);
        snapshot.Should().BeNull();

        GetInterceptor().Clear();

        Result<AuthenticationTokensResponse> refresh = await SendAsync(
            new RefreshTokenCommand(login.Value.RefreshToken));
        refresh.IsFailure.Should().BeTrue();
        refresh.Error.Code.Should().Be("Identity.Authentication.InvalidRefreshToken");
    }

    [Fact]
    public async Task MultipleActiveSessions_ShouldAllBeRevoked_WhenOneConflicts()
    {
        GetInterceptor().Clear();
        LockoutUser user = await CreateUserAsync();

        Result<AuthenticationTokensResponse> first = await SignInAsync(user);
        Result<AuthenticationTokensResponse> second = await SignInAsync(user);
        Result<AuthenticationTokensResponse> third = await SignInAsync(user);
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        third.IsSuccess.Should().BeTrue();

        IReadOnlyList<Guid> sessionIds = await GetActiveSessionIdsAsync(user.UserId);
        sessionIds.Should().HaveCount(3);

        await FailDirectlyAsync(user, attempts: 2);

        GetInterceptor().ForceSessionConflict(
            sessionIds[0],
            ForcedIdentityConcurrencySaveChangesInterceptor.SessionConflictMode.Update);

        await RecordFailureAsync(user.UserId);

        User locked = await QueryUserAsync(user.UserId);
        locked.Status.Should().Be(UserStatus.Locked);
        locked.AccessFailedCount.Should().Be(3);

        foreach (Guid sessionId in sessionIds)
        {
            UserSession session = await QuerySessionAsync(sessionId);
            session.Status.Should().Be(SessionStatus.Revoked);
        }

        foreach (string refreshToken in new[]
                 {
                     first.Value.RefreshToken,
                     second.Value.RefreshToken,
                     third.Value.RefreshToken,
                 })
        {
            Result<AuthenticationTokensResponse> refresh = await SendAsync(
                new RefreshTokenCommand(refreshToken));
            refresh.IsFailure.Should().BeTrue();
        }
    }

    [Fact]
    public async Task LoginThresholdWithConcurrentSessionRevoke_ShouldStillLock()
    {
        GetInterceptor().Clear();
        LockoutUser user = await CreateUserAsync();
        (await SignInAsync(user)).IsSuccess.Should().BeTrue();

        Guid sessionId = await GetLatestSessionIdAsync(user.UserId);

        for (int attempt = 0; attempt < 2; attempt++)
        {
            Result<AuthenticationTokensResponse> failure = await SendAsync(
                new LoginCommand(user.Email, WrongPassword, null, null, null));
            failure.IsFailure.Should().BeTrue();
        }

        GetInterceptor().ForceSessionConflict(
            sessionId,
            ForcedIdentityConcurrencySaveChangesInterceptor.SessionConflictMode.Revoke);

        Result<AuthenticationTokensResponse> threshold = await SendAsync(
            new LoginCommand(user.Email, WrongPassword, null, null, null));
        threshold.IsFailure.Should().BeTrue();

        User locked = await QueryUserAsync(user.UserId);
        locked.Status.Should().Be(UserStatus.Locked);
        locked.AccessFailedCount.Should().Be(3);
        locked.LockoutEndUtc.Should().NotBeNull();
    }

    private ForcedIdentityConcurrencySaveChangesInterceptor GetInterceptor()
        => _factory.Services.GetRequiredService<ForcedIdentityConcurrencySaveChangesInterceptor>();

    private async Task FailDirectlyAsync(LockoutUser user, int attempts)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            await RecordFailureAsync(user.UserId);
        }
    }

    private async Task RecordFailureAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IAccountSecurityStore>();
        await store.RecordAccessFailureAsync(UserId.From(userId));
    }

    private async Task<Result<AuthenticationTokensResponse>> SignInAsync(LockoutUser user)
        => await SendAsync(new LoginCommand(user.Email, IdentityApiFactory.AdminPassword, null, null, null));

    private async Task<LockoutUser> CreateUserAsync()
    {
        string email = $"lockout-concurrency-{Guid.NewGuid():N}@example.com";

        Result<RegisterUserResponse> registered = await SendAsAdminAsync(
            new RegisterUserCommand(email, "Lockout Concurrency User", IdentityApiFactory.AdminPassword));
        registered.IsSuccess.Should().BeTrue();

        Guid userId = registered.Value.UserId;
        (await SendAsAdminAsync(new ActivateUserCommand(userId))).IsSuccess.Should().BeTrue();

        return new LockoutUser(userId, email);
    }

    private async Task<Guid> GetLatestSessionIdAsync(Guid userId)
    {
        IReadOnlyList<Guid> sessionIds = await GetActiveSessionIdsAsync(userId);
        return sessionIds[^1];
    }

    private async Task<IReadOnlyList<Guid>> GetActiveSessionIdsAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        List<UserSession> sessions = await context.Sessions
            .AsNoTracking()
            .Where(candidate => candidate.UserId == UserId.From(userId) && candidate.Status == SessionStatus.Active)
            .ToListAsync();

        return sessions.Select(session => session.Id.Value).ToArray();
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

    private async Task<AuthorizationSnapshot?> ReadSnapshotAsync(Guid userId, Guid sessionId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IAuthorizationSnapshotReader>();
        return await reader.ReadAsync(userId, sessionId, tenantId: null);
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
