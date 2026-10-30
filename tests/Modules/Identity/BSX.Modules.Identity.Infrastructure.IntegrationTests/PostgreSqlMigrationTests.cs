using System.Data.Common;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

/// <summary>
/// Migration verification: applies the full Identity migration chain to an empty PostgreSQL
/// database and validates the generated schema, indexes, concurrency columns and foreign keys.
/// </summary>
public sealed class PostgreSqlMigrationTests
{
    [DockerAvailableFact]
    public async Task Migrations_Should_Apply_AndCreateTheIdentitySchema()
    {
        PostgreSqlIdentityEnvironment environment = await PostgreSqlIdentityEnvironment.GetAsync();
        await using AsyncServiceScope scope = environment.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await context.Database.GetAppliedMigrationsAsync()).Should().ContainInOrder(
            "20261009150630_InitialIdentity",
            "20261015144331_AddAuthentication",
            "20261022123410_AddPlatformOutboxDurability");

        DbConnection connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        (await CountTablesAsync(
            connection,
            "users",
            "roles",
            "tenant_memberships",
            "credentials",
            "user_sessions",
            "session_refresh_tokens",
            "outbox_messages")).Should().Be(7);

        (await CountVersionColumnsAsync(connection)).Should().Be(6);

        (await HasUniqueIndexAsync(connection, "users", "IX_users_Email")).Should().BeTrue();
        (await HasUniqueIndexAsync(connection, "roles", "IX_roles_TenantId_Name")).Should().BeTrue();
        (await HasUniqueIndexAsync(connection, "roles", "IX_roles_Name")).Should().BeTrue();
        (await HasUniqueIndexAsync(connection, "tenant_memberships", "IX_tenant_memberships_TenantId_UserId"))
            .Should().BeTrue();
        (await HasUniqueIndexAsync(connection, "outbox_messages", "IX_outbox_messages_EventId")).Should().BeTrue();

        (await GetDeleteRuleAsync(connection, "FK_credentials_users_UserId")).Should().Be("CASCADE");
        (await GetDeleteRuleAsync(connection, "FK_session_refresh_tokens_user_sessions_SessionId")).Should().Be("CASCADE");
        (await GetDeleteRuleAsync(connection, "FK_tenant_membership_roles_tenant_memberships_MembershipId"))
            .Should().Be("CASCADE");
    }

    private static async Task<int> CountTablesAsync(DbConnection connection, params string[] tables)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)
            FROM information_schema.tables
            WHERE table_schema = 'identity'
              AND table_name = ANY(@tables)
            """;
        AddParameter(command, "tables", tables);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<int> CountVersionColumnsAsync(DbConnection connection)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)
            FROM information_schema.columns
            WHERE table_schema = 'identity'
              AND column_name = 'Version'
              AND table_name IN ('users', 'roles', 'tenant_memberships', 'service_accounts', 'api_keys', 'user_sessions')
            """;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<bool> HasUniqueIndexAsync(DbConnection connection, string table, string index)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM pg_indexes
                WHERE schemaname = 'identity'
                  AND tablename = @table
                  AND indexname = @index
                  AND indexdef LIKE '%UNIQUE%'
            )
            """;
        AddParameter(command, "table", table);
        AddParameter(command, "index", index);
        return Convert.ToBoolean(await command.ExecuteScalarAsync());
    }

    private static async Task<string?> GetDeleteRuleAsync(DbConnection connection, string constraintName)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT rc.delete_rule
            FROM information_schema.referential_constraints rc
            JOIN information_schema.table_constraints tc
              ON tc.constraint_name = rc.constraint_name
             AND tc.constraint_schema = rc.constraint_schema
            WHERE tc.constraint_schema = 'identity'
              AND tc.constraint_name = @name
            """;
        AddParameter(command, "name", constraintName);
        return await command.ExecuteScalarAsync() as string;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
