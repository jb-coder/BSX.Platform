using BSX.BuildingBlocks.Persistence;
using BSX.Contracts.Permissions;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Authentication;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

/// <summary>
/// Security parity: validates the lockout hardening, permission scope model and authorization
/// revalidation dependencies against a real PostgreSQL instance (ADR-020, ADR-021, ADR-022).
/// </summary>
public sealed class PostgreSqlSecurityParityTests
{
    [DockerAvailableFact]
    public async Task Lockout_Should_PersistAndRevokeSessions_OnPostgreSql()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        Guid userId;
        Guid[] sessionIds;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            UserSession first = PostgreSqlIdentityTestSupport.StartSession(user);
            UserSession second = PostgreSqlIdentityTestSupport.StartSession(user);

            await users.AddAsync(user);
            await sessions.AddAsync(first);
            await sessions.AddAsync(second);
            await unitOfWork.SaveChangesAsync();

            userId = user.Id.Value;
            sessionIds = [first.Id.Value, second.Id.Value];
        }

        string preLockoutStamp = (await GetUserAsync(environment, userId)).SecurityStamp.Value;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IAccountSecurityStore>();

            await store.RecordAccessFailureAsync(UserId.From(userId));
            await store.RecordAccessFailureAsync(UserId.From(userId));
            await store.RecordAccessFailureAsync(UserId.From(userId));
        }

        User locked = await GetUserAsync(environment, userId);
        locked.Status.Should().Be(UserStatus.Locked);
        locked.AccessFailedCount.Should().Be(3);
        locked.LockoutEndUtc.Should().NotBeNull();
        locked.SecurityStamp.Value.Should().NotBe(preLockoutStamp);

        foreach (Guid sessionId in sessionIds)
        {
            UserSession session = await GetSessionAsync(environment, sessionId);
            session.Status.Should().Be(SessionStatus.Revoked);
            session.RevokedReason.Should().Be("account_locked");
        }
    }

    [DockerAvailableFact]
    public async Task ConcurrentFailures_Should_BeSerialized_ByTheAdvisoryLock()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        Guid userId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            await users.AddAsync(user);
            await unitOfWork.SaveChangesAsync();
            userId = user.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var store = new AccountSecurityStore(
                scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new LockoutOptions { MaxFailedAttempts = 100, LockoutDurationMinutes = 15 }),
                TimeProvider.System,
                NullLogger<AccountSecurityStore>.Instance);

            await Task.WhenAll(Enumerable.Range(0, 10).Select(
                _ => store.RecordAccessFailureAsync(UserId.From(userId))));
        }

        User userAfter = await GetUserAsync(environment, userId);
        userAfter.Status.Should().Be(UserStatus.Active);
        userAfter.AccessFailedCount.Should().Be(10, "the PostgreSQL advisory lock must serialize increments");
    }

    [DockerAvailableFact]
    public async Task ConcurrentSessionRevoke_ShouldNotRollBackLockout_OnPostgreSql()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        Guid userId;
        Guid sessionId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            UserSession session = PostgreSqlIdentityTestSupport.StartSession(user);
            await users.AddAsync(user);
            await sessions.AddAsync(session);
            await unitOfWork.SaveChangesAsync();

            userId = user.Id.Value;
            sessionId = session.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IAccountSecurityStore>();
            await store.RecordAccessFailureAsync(UserId.From(userId));
            await store.RecordAccessFailureAsync(UserId.From(userId));
        }

        string preLockoutStamp = (await GetUserAsync(environment, userId)).SecurityStamp.Value;

        // Hold a row lock on the session while the threshold failure is recorded. The lockout write
        // must commit independently; the session cleanup then observes a concurrency conflict.
        await using AsyncServiceScope blockerScope = environment.Services.CreateAsyncScope();
        var blockerSessions = blockerScope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
        var blockerUnitOfWork = blockerScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await using IDatabaseTransaction blockerTransaction = await blockerUnitOfWork.BeginTransactionAsync();
        UserSession blockerSession = (await blockerSessions.GetByIdAsync(SessionId.From(sessionId)))!;
        blockerSession.Revoke(DateTimeOffset.UtcNow, "concurrent").IsSuccess.Should().BeTrue();
        await blockerUnitOfWork.SaveChangesAsync();

        await using (AsyncServiceScope storeScope = environment.Services.CreateAsyncScope())
        {
            var store = storeScope.ServiceProvider.GetRequiredService<IAccountSecurityStore>();
            Task lockoutTask = store.RecordAccessFailureAsync(UserId.From(userId));

            await Task.Delay(TimeSpan.FromMilliseconds(300));
            await blockerTransaction.CommitAsync();
            await lockoutTask;
        }

        User locked = await GetUserAsync(environment, userId);
        locked.Status.Should().Be(UserStatus.Locked);
        locked.AccessFailedCount.Should().Be(3);
        locked.SecurityStamp.Value.Should().NotBe(preLockoutStamp);

        UserSession revoked = await GetSessionAsync(environment, sessionId);
        revoked.Status.Should().Be(SessionStatus.Revoked);
    }

    [DockerAvailableFact]
    public async Task PermissionResolution_Should_EnforceScopeBoundaries_OnPostgreSql()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        var tenantId = TenantId.New();
        Guid userId;
        Guid membershipId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();

            Role platformRole = Role.Create(
                RoleId.New(),
                tenantId: null,
                RoleName.Create($"Platform {Guid.NewGuid():N}").Value,
                [
                    Permission.Create("identity.users.read").Value,
                    Permission.Create("crm.customers.read").Value,
                ],
                isSystem: true).Value;
            user.AssignPlatformRole(platformRole.Id);

            Role tenantRole = Role.Create(
                RoleId.New(),
                tenantId,
                RoleName.Create($"Tenant {Guid.NewGuid():N}").Value,
                [
                    Permission.Create("sales.leads.read").Value,
                    Permission.Create("identity.roles.read").Value,
                    // Deliberately tampered: a tenant role must not grant a platform-scoped permission.
                    Permission.Create("identity.users.manage-status").Value,
                ]).Value;

            var membership = TenantMembership
                .Invite(MembershipId.New(), tenantId, user.Id, null, TestData.Now).Value;
            membership.Accept(TestData.Now);
            membership.AssignRole(tenantRole.Id);

            await users.AddAsync(user);
            await roles.AddAsync(platformRole);
            await roles.AddAsync(tenantRole);
            await memberships.AddAsync(membership);
            await unitOfWork.SaveChangesAsync();

            userId = user.Id.Value;
            membershipId = membership.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();
            var resolver = scope.ServiceProvider.GetRequiredService<PermissionResolver>();
            var catalog = scope.ServiceProvider.GetRequiredService<PermissionCatalog>();

            catalog.EnsureValid();

            User user = (await users.GetByIdAsync(UserId.From(userId)))!;
            TenantMembership membership = (await memberships.GetByIdAsync(MembershipId.From(membershipId)))!;
            IReadOnlyCollection<string> permissions = await resolver.ResolveAsync(user, membership);

            permissions.Should().Contain([
                "identity.users.read",
                "crm.customers.read",
                "sales.leads.read",
                "identity.roles.read",
            ]);
            permissions.Should().NotContain("identity.users.manage-status");
        }
    }

    [DockerAvailableFact]
    public async Task AuthorizationSnapshot_Should_DependOnStatusSessionAndStamp_OnPostgreSql()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        var tenantId = TenantId.New();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid userId;
        Guid firstSessionId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            Role role = TestData.CreateRole(tenantId, $"Reader {Guid.NewGuid():N}", "crm.customers.read");
            var membership = TenantMembership
                .Invite(MembershipId.New(), tenantId, user.Id, null, TestData.Now).Value;
            membership.Accept(TestData.Now);
            membership.AssignRole(role.Id);

            UserSession session = PostgreSqlIdentityTestSupport.StartSession(
                user,
                tenantId,
                nowUtc: now);

            await users.AddAsync(user);
            await roles.AddAsync(role);
            await memberships.AddAsync(membership);
            await sessions.AddAsync(session);
            await unitOfWork.SaveChangesAsync();

            userId = user.Id.Value;
            firstSessionId = session.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var reader = scope.ServiceProvider.GetRequiredService<IAuthorizationSnapshotReader>();
            AuthorizationSnapshot? snapshot = await reader.ReadAsync(userId, firstSessionId, tenantId.Value);

            snapshot.Should().NotBeNull();
            snapshot!.Permissions.Should().Contain("crm.customers.read");
            snapshot.SecurityStamp.Should().Be((await GetUserAsync(environment, userId)).SecurityStamp.Value);
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            UserSession session = (await sessions.GetByIdAsync(SessionId.From(firstSessionId)))!;
            session.Revoke(now, "revoked").IsSuccess.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var reader = scope.ServiceProvider.GetRequiredService<IAuthorizationSnapshotReader>();
            (await reader.ReadAsync(userId, firstSessionId, tenantId.Value)).Should().BeNull();
        }

        Guid secondSessionId;
        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = (await users.GetByIdAsync(UserId.From(userId)))!;
            UserSession session = PostgreSqlIdentityTestSupport.StartSession(user, tenantId, nowUtc: now);
            await sessions.AddAsync(session);
            await unitOfWork.SaveChangesAsync();
            secondSessionId = session.Id.Value;

            user.SetPassword(PasswordHash.From("rotated", "test"), now).IsSuccess.Should().BeTrue();
            await unitOfWork.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var reader = scope.ServiceProvider.GetRequiredService<IAuthorizationSnapshotReader>();
            (await reader.ReadAsync(userId, secondSessionId, tenantId.Value)).Should().BeNull(
                "a rotated security stamp must invalidate the session");
        }

        Guid thirdSessionId;
        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = (await users.GetByIdAsync(UserId.From(userId)))!;
            UserSession session = PostgreSqlIdentityTestSupport.StartSession(user, tenantId, nowUtc: now);
            await sessions.AddAsync(session);
            await unitOfWork.SaveChangesAsync();
            thirdSessionId = session.Id.Value;

            user.RecordAccessFailure(now, maxAttempts: 1).IsSuccess.Should().BeTrue();
            await unitOfWork.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var reader = scope.ServiceProvider.GetRequiredService<IAuthorizationSnapshotReader>();
            (await reader.ReadAsync(userId, thirdSessionId, tenantId.Value)).Should().BeNull(
                "a locked account must be denied even when the session row is active");
        }
    }

    [DockerAvailableFact]
    public async Task RevokeSession_Should_PersistDurably_OnPostgreSql()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        Guid sessionId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            UserSession session = PostgreSqlIdentityTestSupport.StartSession(user);
            await users.AddAsync(user);
            await sessions.AddAsync(session);
            await unitOfWork.SaveChangesAsync();
            sessionId = session.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IAccountSecurityStore>();
            await store.RevokeSessionAsync(SessionId.From(sessionId), "security_stamp_changed");
        }

        UserSession revoked = await GetSessionAsync(environment, sessionId);
        revoked.Status.Should().Be(SessionStatus.Revoked);
        revoked.RevokedReason.Should().Be("security_stamp_changed");
    }

    private static async Task<User> GetUserAsync(PostgreSqlIdentityEnvironment environment, Guid userId)
    {
        await using AsyncServiceScope scope = environment.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        return (await users.GetByIdAsync(UserId.From(userId)))!;
    }

    private static async Task<UserSession> GetSessionAsync(
        PostgreSqlIdentityEnvironment environment,
        Guid sessionId)
    {
        await using AsyncServiceScope scope = environment.Services.CreateAsyncScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
        return (await sessions.GetByIdAsync(SessionId.From(sessionId)))!;
    }
}
