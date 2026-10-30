using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using Npgsql;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;

/// <summary>Small helpers shared by the Identity PostgreSQL parity tests.</summary>
internal static class PostgreSqlIdentityTestSupport
{
    internal static string NewEmail() => $"pg-{Guid.NewGuid():N}@example.com";

    internal static User CreateActiveUser(string? email = null)
    {
        User user = TestData.CreateUser(email ?? NewEmail());
        user.Activate(TestData.Now).IsSuccess.Should().BeTrue();
        return user;
    }

    internal static UserSession StartSession(
        User user,
        TenantId? tenantId = null,
        string? refreshTokenHash = null,
        DateTimeOffset? nowUtc = null,
        SessionPolicy? policy = null)
    {
        DateTimeOffset now = nowUtc ?? DateTimeOffset.UtcNow;

        return UserSession.Start(
            SessionId.New(),
            user.Id,
            tenantId,
            user.SecurityStamp.Value,
            RefreshTokenHash.From(refreshTokenHash ?? $"hash-{Guid.NewGuid():N}"),
            SessionMetadata.Create("postgres-parity", "127.0.0.1"),
            now,
            policy ?? SessionPolicy.Default).Value;
    }

    internal static async Task<PostgresException> CatchPostgresExceptionAsync(Func<Task> action)
    {
        Exception exception = (await action.Should().ThrowAsync<Exception>()).Which;
        PostgresException? postgres = exception as PostgresException ?? exception.InnerException as PostgresException;

        postgres.Should().NotBeNull("the failure must be raised by PostgreSQL");
        return postgres!;
    }
}
