using BSX.Modules.Crm.Infrastructure.Persistence;
using BSX.Modules.Crm.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;

/// <summary>
/// A real PostgreSQL instance (Testcontainers) migrated with the CRM migrations, shared by every
/// parity test in the process. Tests isolate themselves with unique tenants, so they can run in
/// parallel against one database.
/// </summary>
internal sealed class PostgreSqlCrmEnvironment
{
    private static readonly Lazy<Task<PostgreSqlCrmEnvironment>> Shared =
        new(CreateAsync, isThreadSafe: true);

    private readonly PostgreSqlContainer _container;
    private readonly DbContextOptions<CrmDbContext> _options;

    private PostgreSqlCrmEnvironment(PostgreSqlContainer container, DbContextOptions<CrmDbContext> options)
    {
        _container = container;
        _options = options;
    }

    /// <summary>Gets the shared, migrated PostgreSQL environment (starts it on first use).</summary>
    internal static Task<PostgreSqlCrmEnvironment> GetAsync() => Shared.Value;

    /// <summary>Gets the underlying container, kept alive for the test process lifetime.</summary>
    internal PostgreSqlContainer Container => _container;

    /// <summary>Creates a context scoped to the requested tenant (or system scope).</summary>
    internal CrmDbContext CreateContext(Guid? tenantId, bool isSystem = false)
        => new(_options, new TestCrmTenantScope(tenantId, isSystem));

    private static async Task<PostgreSqlCrmEnvironment> CreateAsync()
    {
        PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("bsx_crm_tests")
            .WithUsername("bsx")
            .WithPassword("bsx")
            .Build();

        await container.StartAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(
                container.GetConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "crm"))
            .AddInterceptors(new CrmAuditingSaveChangesInterceptor(TimeProvider.System, new TestCurrentUser()))
            .Options;

        await using (var context = new CrmDbContext(options, new TestCrmTenantScope(null, isSystem: true)))
        {
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }

        return new PostgreSqlCrmEnvironment(container, options);
    }
}
