using BSX.Modules.Sales.Infrastructure.Persistence;
using BSX.Modules.Sales.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;

/// <summary>
/// A real PostgreSQL instance (Testcontainers) migrated with the Sales migrations, shared by every
/// parity test in the process. Tests isolate themselves with unique tenants.
/// </summary>
internal sealed class PostgreSqlSalesEnvironment
{
    private static readonly Lazy<Task<PostgreSqlSalesEnvironment>> Shared =
        new(CreateAsync, isThreadSafe: true);

    private readonly PostgreSqlContainer _container;
    private readonly DbContextOptions<SalesDbContext> _options;

    private PostgreSqlSalesEnvironment(
        PostgreSqlContainer container,
        DbContextOptions<SalesDbContext> options)
    {
        _container = container;
        _options = options;
    }

    internal static Task<PostgreSqlSalesEnvironment> GetAsync() => Shared.Value;

    internal PostgreSqlContainer Container => _container;

    internal SalesDbContext CreateContext(Guid? tenantId, bool isSystem = false)
        => new(_options, new TestSalesTenantScope(tenantId, isSystem));

    private static async Task<PostgreSqlSalesEnvironment> CreateAsync()
    {
        PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("bsx_sales_tests")
            .WithUsername("bsx")
            .WithPassword("bsx")
            .Build();

        await container.StartAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(
                container.GetConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "sales"))
            .AddInterceptors(
                new SalesAuditingSaveChangesInterceptor(TimeProvider.System, new TestCurrentUser()),
                new CrmReferenceValidationInterceptor(new TestCrmReferenceValidator()))
            .Options;

        await using (var context = new SalesDbContext(options, new TestSalesTenantScope(null, isSystem: true)))
        {
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }

        return new PostgreSqlSalesEnvironment(container, options);
    }
}
