using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Memberships;
using BSX.Modules.Identity.Domain.Roles;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

/// <summary>
/// Provider parity: validates the Identity schema, migrations, constraints, concurrency tokens,
/// transactions and aggregate round-trips against a real PostgreSQL instance (the production
/// provider, ADR-004).
/// </summary>
public sealed class PostgreSqlParityTests
{
    [DockerAvailableFact]
    public async Task User_Should_RoundTrip_AndEnforceUniqueEmail()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        string email = PostgreSqlIdentityTestSupport.NewEmail();

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser(email);
            await users.AddAsync(user);
            await unitOfWork.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            User? loaded = await users.GetByEmailAsync(Email.Create(email).Value);

            loaded.Should().NotBeNull();
            loaded!.Status.Should().Be(UserStatus.Active);
            loaded.HasCredential(CredentialType.Password).Should().BeTrue();
            loaded.SecurityStamp.Value.Should().NotBeNullOrWhiteSpace();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User duplicate = PostgreSqlIdentityTestSupport.CreateActiveUser(email);
            await users.AddAsync(duplicate);

            Func<Task> act = () => unitOfWork.SaveChangesAsync();
            PostgresException exception = await PostgreSqlIdentityTestSupport.CatchPostgresExceptionAsync(act);

            exception.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            exception.ConstraintName.Should().Be("IX_users_Email");
        }
    }

    [DockerAvailableFact]
    public async Task User_Should_SupportUpdateActivateAndDeactivate()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        string originalEmail = PostgreSqlIdentityTestSupport.NewEmail();
        string updatedEmail = PostgreSqlIdentityTestSupport.NewEmail();
        Guid userId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = TestData.CreateUser(originalEmail);
            await users.AddAsync(user);
            await unitOfWork.SaveChangesAsync();

            user.Activate(TestData.Now).IsSuccess.Should().BeTrue();
            string activatedStamp = user.SecurityStamp.Value;
            user.ChangeEmail(Email.Create(updatedEmail).Value).IsSuccess.Should().BeTrue();
            user.ChangeName(PersonName.Create("Updated Name").Value).IsSuccess.Should().BeTrue();
            await unitOfWork.SaveChangesAsync();

            user.SecurityStamp.Value.Should().NotBe(activatedStamp);
            userId = user.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            User loaded = (await users.GetByIdAsync(UserId.From(userId)))!;

            loaded.Status.Should().Be(UserStatus.Active);
            loaded.Email.Value.Should().Be(updatedEmail);
            loaded.Name.Value.Should().Be("Updated Name");

            loaded.Deactivate().IsSuccess.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            User loaded = (await users.GetByIdAsync(UserId.From(userId)))!;

            loaded.Status.Should().Be(UserStatus.Disabled);
            loaded.Email.Value.Should().Be(updatedEmail);
        }
    }

    [DockerAvailableFact]
    public async Task Role_Should_RoundTripPermissions_AndEnforceNamePerTenant()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        var tenantA = TenantId.New();
        var tenantB = TenantId.New();
        string roleName = $"Manager-{Guid.NewGuid():N}";

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            Role role = TestData.CreateRole(tenantA, roleName, "crm.customers.read");
            await roles.AddAsync(role);
            await unitOfWork.SaveChangesAsync();

            role.Grant(Permission.Create("crm.customers.create").Value).IsSuccess.Should().BeTrue();
            role.Revoke(Permission.Create("crm.customers.read").Value).IsSuccess.Should().BeTrue();
            await unitOfWork.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            Role loaded = (await roles.ListByTenantAsync(tenantA)).Single(role => role.Name.Value == roleName);

            loaded.Permissions.Select(permission => permission.Code).Should().BeEquivalentTo(["crm.customers.create"]);
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await roles.AddAsync(TestData.CreateRole(tenantA, roleName, "crm.customers.read"));

            Func<Task> act = () => unitOfWork.SaveChangesAsync();
            PostgresException exception = await PostgreSqlIdentityTestSupport.CatchPostgresExceptionAsync(act);

            exception.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            exception.ConstraintName.Should().Be("IX_roles_TenantId_Name");
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // The same name is allowed in a different tenant.
            await roles.AddAsync(TestData.CreateRole(tenantB, roleName, "sales.leads.read"));
            await unitOfWork.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            string systemRoleName = $"System Role {Guid.NewGuid():N}";
            Role firstSystemRole = Role.Create(
                RoleId.New(),
                tenantId: null,
                RoleName.Create(systemRoleName).Value,
                [Permission.Create("identity.users.read").Value],
                isSystem: true).Value;
            await roles.AddAsync(firstSystemRole);
            await unitOfWork.SaveChangesAsync();

            Role duplicateSystemRole = Role.Create(
                RoleId.New(),
                tenantId: null,
                RoleName.Create(systemRoleName).Value,
                [Permission.Create("identity.users.read").Value],
                isSystem: true).Value;
            await roles.AddAsync(duplicateSystemRole);

            Func<Task> act = () => unitOfWork.SaveChangesAsync();
            PostgresException exception = await PostgreSqlIdentityTestSupport.CatchPostgresExceptionAsync(act);

            exception.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            exception.ConstraintName.Should().Be("IX_roles_Name");
        }
    }

    [DockerAvailableFact]
    public async Task Membership_Should_RoundTripRoles_AndSupportMultiTenantHandling()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        var tenantA = TenantId.New();
        var tenantB = TenantId.New();
        Guid userId;
        Guid membershipAId;
        Guid membershipBId;
        Guid roleAId;
        Guid roleBId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            Role roleA = TestData.CreateRole(tenantA, $"Tenant A {Guid.NewGuid():N}", "crm.customers.read");
            Role roleB = TestData.CreateRole(tenantB, $"Tenant B {Guid.NewGuid():N}", "sales.leads.read");

            var membershipA = TenantMembership
                .Invite(MembershipId.New(), tenantA, user.Id, null, TestData.Now).Value;
            membershipA.Accept(TestData.Now);
            membershipA.AssignRole(roleA.Id);

            var membershipB = TenantMembership
                .Invite(MembershipId.New(), tenantB, user.Id, null, TestData.Now).Value;
            membershipB.Accept(TestData.Now);
            membershipB.AssignRole(roleB.Id);

            await users.AddAsync(user);
            await roles.AddAsync(roleA);
            await roles.AddAsync(roleB);
            await memberships.AddAsync(membershipA);
            await memberships.AddAsync(membershipB);
            await unitOfWork.SaveChangesAsync();

            userId = user.Id.Value;
            membershipAId = membershipA.Id.Value;
            membershipBId = membershipB.Id.Value;
            roleAId = roleA.Id.Value;
            roleBId = roleB.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();
            IReadOnlyCollection<TenantMembership> loaded = await memberships.ListByUserAsync(UserId.From(userId));

            loaded.Should().HaveCount(2);
            loaded.Single(membership => membership.TenantId == tenantA).RoleIds
                .Should().Contain(RoleId.From(roleAId));
            loaded.Single(membership => membership.TenantId == tenantB).RoleIds
                .Should().Contain(RoleId.From(roleBId));
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            TenantMembership membershipA = (await memberships.GetByIdAsync(MembershipId.From(membershipAId)))!;
            membershipA.RemoveRole(RoleId.From(roleAId)).IsSuccess.Should().BeTrue();
            membershipA.AssignRole(RoleId.From(roleBId)).IsSuccess.Should().BeTrue();
            await memberships.TrackRoleChangesAsync(membershipA);
            await unitOfWork.SaveChangesAsync();

            TenantMembership membershipB = (await memberships.GetByIdAsync(MembershipId.From(membershipBId)))!;
            membershipB.Remove().IsSuccess.Should().BeTrue();
            await unitOfWork.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();

            TenantMembership membershipA = (await memberships.GetByIdAsync(MembershipId.From(membershipAId)))!;
            membershipA.RoleIds.Should().ContainSingle().Which.Should().Be(RoleId.From(roleBId));

            TenantMembership membershipB = (await memberships.GetByIdAsync(MembershipId.From(membershipBId)))!;
            membershipB.Status.Should().Be(MembershipStatus.Removed);
            membershipB.RoleIds.Should().BeEmpty();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var memberships = scope.ServiceProvider.GetRequiredService<ITenantMembershipRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var duplicate = TenantMembership
                .Invite(MembershipId.New(), tenantA, UserId.From(userId), null, TestData.Now).Value;
            duplicate.Accept(TestData.Now);
            await memberships.AddAsync(duplicate);

            Func<Task> act = () => unitOfWork.SaveChangesAsync();
            PostgresException exception = await PostgreSqlIdentityTestSupport.CatchPostgresExceptionAsync(act);

            exception.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
            exception.ConstraintName.Should().Be("IX_tenant_memberships_TenantId_UserId");
        }
    }

    [DockerAvailableFact]
    public async Task Session_Should_RoundTripRotationRevocation_AndSecurityStamp()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string firstHash = $"first-{Guid.NewGuid():N}";
        string secondHash = $"second-{Guid.NewGuid():N}";
        Guid sessionId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            UserSession session = PostgreSqlIdentityTestSupport.StartSession(
                user,
                refreshTokenHash: firstHash,
                nowUtc: now);

            await users.AddAsync(user);
            await sessions.AddAsync(session);
            await unitOfWork.SaveChangesAsync();

            session.SecurityStamp.Should().Be(user.SecurityStamp.Value);
            session.Rotate(
                RefreshTokenHash.From(firstHash),
                RefreshTokenHash.From(secondHash),
                now.AddMinutes(1),
                SessionPolicy.Default).IsSuccess.Should().BeTrue();
            await unitOfWork.SaveChangesAsync();

            sessionId = session.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            UserSession loaded = (await sessions.GetByIdAsync(SessionId.From(sessionId)))!;

            loaded.Status.Should().Be(SessionStatus.Active);
            loaded.CurrentToken.Should().NotBeNull();
            loaded.CurrentToken!.Hash.Value.Should().Be(secondHash);
            loaded.RefreshTokens.Should().ContainSingle(token =>
                token.Hash.Value == firstHash && token.UsedOnUtc != null);
            loaded.LastRefreshedOnUtc.Should().BeAfter(now);

            loaded.Revoke(now.AddMinutes(2), "logout").IsSuccess.Should().BeTrue();
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            UserSession loaded = (await sessions.GetByIdAsync(SessionId.From(sessionId)))!;

            loaded.Status.Should().Be(SessionStatus.Revoked);
            loaded.RevokedReason.Should().Be("logout");
            loaded.RevokedOnUtc.Should().NotBeNull();
        }
    }

    [DockerAvailableFact]
    public async Task Session_Should_DetectRefreshTokenReuse_OutsideTheGraceWindow()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string firstHash = $"first-{Guid.NewGuid():N}";
        string secondHash = $"second-{Guid.NewGuid():N}";
        SessionPolicy policy = SessionPolicy.Default with { GraceWindow = TimeSpan.Zero };
        Guid sessionId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            UserSession session = PostgreSqlIdentityTestSupport.StartSession(
                user,
                refreshTokenHash: firstHash,
                nowUtc: now,
                policy: policy);

            await users.AddAsync(user);
            await sessions.AddAsync(session);
            await unitOfWork.SaveChangesAsync();

            session.Rotate(
                RefreshTokenHash.From(firstHash),
                RefreshTokenHash.From(secondHash),
                now.AddMinutes(1),
                policy).IsSuccess.Should().BeTrue();
            await unitOfWork.SaveChangesAsync();

            var reuse = session.Rotate(
                RefreshTokenHash.From(firstHash),
                RefreshTokenHash.From($"third-{Guid.NewGuid():N}"),
                now.AddMinutes(2),
                policy);

            reuse.IsFailure.Should().BeTrue();
            reuse.Error.Code.Should().Be("Identity.Session.RefreshTokenReuse");
            await unitOfWork.SaveChangesAsync();

            sessionId = session.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            UserSession loaded = (await sessions.GetByIdAsync(SessionId.From(sessionId)))!;

            loaded.Status.Should().Be(SessionStatus.Revoked);
            loaded.RevokedReason.Should().Be("refresh_token_reuse");
        }
    }

    [DockerAvailableFact]
    public async Task LogoutAll_Should_RevokeEveryActiveSession()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        Guid userId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            await users.AddAsync(user);
            await sessions.AddAsync(PostgreSqlIdentityTestSupport.StartSession(user));
            await sessions.AddAsync(PostgreSqlIdentityTestSupport.StartSession(user));
            await sessions.AddAsync(PostgreSqlIdentityTestSupport.StartSession(user));
            await unitOfWork.SaveChangesAsync();

            userId = user.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var handler = new BSX.Modules.Identity.Application.Authentication.LogoutAll.LogoutAllCommandHandler(
                sessions,
                new TestCurrentUser(userId),
                unitOfWork,
                TimeProvider.System);

            var result = await handler.HandleAsync(
                new BSX.Modules.Identity.Application.Authentication.LogoutAll.LogoutAllCommand());

            result.IsSuccess.Should().BeTrue();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            IReadOnlyCollection<UserSession> loaded = await sessions.ListByUserAsync(UserId.From(userId));

            loaded.Should().HaveCount(3);
            loaded.Should().OnlyContain(session => session.Status == SessionStatus.Revoked);
            loaded.Should().OnlyContain(session => session.RevokedReason == "logout_all");
        }
    }

    [DockerAvailableFact]
    public async Task Concurrency_Should_DetectStaleUserAndSessionUpdates()
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

        await using AsyncServiceScope firstScope = environment.Services.CreateAsyncScope();
        await using AsyncServiceScope secondScope = environment.Services.CreateAsyncScope();

        var firstUsers = firstScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var secondUsers = secondScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var firstSessions = firstScope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
        var secondSessions = secondScope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
        var firstUnitOfWork = firstScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var secondUnitOfWork = secondScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        User firstUser = (await firstUsers.GetByIdAsync(UserId.From(userId)))!;
        User secondUser = (await secondUsers.GetByIdAsync(UserId.From(userId)))!;

        firstUser.ChangeName(PersonName.Create("Postgres Winner").Value).IsSuccess.Should().BeTrue();
        await firstUnitOfWork.SaveChangesAsync();

        secondUser.ChangeName(PersonName.Create("Postgres Loser").Value).IsSuccess.Should().BeTrue();
        Func<Task> userAct = () => secondUnitOfWork.SaveChangesAsync();
        await userAct.Should().ThrowAsync<ConcurrencyConflictException>();

        // The failed save left the stale user tracked; drop it before exercising session concurrency.
        secondScope.ServiceProvider.GetRequiredService<IdentityDbContext>().ChangeTracker.Clear();

        UserSession firstSession = (await firstSessions.GetByIdAsync(SessionId.From(sessionId)))!;
        UserSession secondSession = (await secondSessions.GetByIdAsync(SessionId.From(sessionId)))!;

        firstSession.Revoke(DateTimeOffset.UtcNow, "first").IsSuccess.Should().BeTrue();
        await firstUnitOfWork.SaveChangesAsync();

        secondSession.Revoke(DateTimeOffset.UtcNow, "second").IsSuccess.Should().BeTrue();
        Func<Task> sessionAct = () => secondUnitOfWork.SaveChangesAsync();
        await sessionAct.Should().ThrowAsync<ConcurrencyConflictException>();

        secondScope.ServiceProvider.GetRequiredService<IdentityDbContext>().ChangeTracker.Clear();
        User persisted = (await secondUsers.GetByIdAsync(UserId.From(userId)))!;
        persisted.Name.Value.Should().Be("Postgres Winner");
    }

    [DockerAvailableFact]
    public async Task Transactions_Should_RollBack_AndPersistTheOutboxAtomically()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        string rolledBackEmail = PostgreSqlIdentityTestSupport.NewEmail();
        string committedEmail = PostgreSqlIdentityTestSupport.NewEmail();

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await using IDatabaseTransaction transaction = await unitOfWork.BeginTransactionAsync();
            User user = PostgreSqlIdentityTestSupport.CreateActiveUser(rolledBackEmail);
            await users.AddAsync(user);
            await unitOfWork.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            (await users.GetByEmailAsync(Email.Create(rolledBackEmail).Value)).Should().BeNull();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await using IDatabaseTransaction transaction = await unitOfWork.BeginTransactionAsync();
            User user = PostgreSqlIdentityTestSupport.CreateActiveUser(committedEmail);
            await users.AddAsync(user);
            await unitOfWork.SaveChangesAndDispatchAsync();
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            (await users.GetByEmailAsync(Email.Create(committedEmail).Value)).Should().NotBeNull();
            (await context.OutboxMessages.CountAsync()).Should().BeGreaterThan(0);
            (await context.OutboxMessages.AnyAsync(message => message.EventType.Contains("UserRegistered")))
                .Should().BeTrue();
        }
    }

    [DockerAvailableFact]
    public async Task SoftDelete_Should_RetainRows_AndExcludeThemFromQueries()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        Guid userId;

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            User user = PostgreSqlIdentityTestSupport.CreateActiveUser();
            await users.AddAsync(user);
            await unitOfWork.SaveChangesAsync();

            context.Users.Remove(user);
            await unitOfWork.SaveChangesAsync();
            userId = user.Id.Value;
        }

        await using (AsyncServiceScope scope = environment.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            (await users.GetByIdAsync(UserId.From(userId))).Should().BeNull();

            User deleted = await context.Users
                .IgnoreQueryFilters()
                .SingleAsync(user => user.Id == UserId.From(userId));
            context.Entry(deleted).Property("IsDeleted").CurrentValue.Should().Be(true);
        }
    }
}
