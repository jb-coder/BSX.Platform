using BSX.BuildingBlocks.DependencyInjection;
using BSX.Contracts.Permissions;
using BSX.Modules.Crm.Contracts.Permissions;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.DependencyInjection;
using BSX.Modules.Identity.Infrastructure.DependencyInjection;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Sales.Contracts.Permissions;
using BSX.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;

/// <summary>
/// A real PostgreSQL instance (Testcontainers) migrated with the full Identity migration chain and
/// wired through the production infrastructure registration. Shared by every parity test in the
/// process; tests isolate themselves with unique identifiers.
/// </summary>
internal sealed class PostgreSqlIdentityEnvironment
{
    private static readonly Lazy<Task<PostgreSqlIdentityEnvironment>> Shared =
        new(CreateAsync, isThreadSafe: true);

    private readonly PostgreSqlContainer _container;
    private readonly ServiceProvider _services;

    private PostgreSqlIdentityEnvironment(
        PostgreSqlContainer container,
        ServiceProvider services,
        string connectionString)
    {
        _container = container;
        _services = services;
        ConnectionString = connectionString;
    }

    /// <summary>Gets the shared, migrated PostgreSQL environment (starts it on first use).</summary>
    internal static Task<PostgreSqlIdentityEnvironment> GetAsync() => Shared.Value;

    /// <summary>Gets the underlying container, kept alive for the test process lifetime.</summary>
    internal PostgreSqlContainer Container => _container;

    /// <summary>Gets the connection string of the migrated test database.</summary>
    internal string ConnectionString { get; }

    /// <summary>Gets the production-shaped Identity service provider.</summary>
    internal IServiceProvider Services => _services;

    private static async Task<PostgreSqlIdentityEnvironment> CreateAsync()
    {
        PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("bsx_identity_tests")
            .WithUsername("bsx")
            .WithPassword("bsx")
            .Build();

        await container.StartAsync().ConfigureAwait(false);
        string connectionString = container.GetConnectionString();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
            .Options;

        await using (var context = new IdentityDbContext(options))
        {
            await context.Database.MigrateAsync().ConfigureAwait(false);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityApplication();
        services.Configure<LockoutOptions>(lockout =>
        {
            lockout.MaxFailedAttempts = 3;
            lockout.LockoutDurationMinutes = 15;
        });
        services.AddIdentityInfrastructure(connectionString);
        services.AddCompositeUnitOfWork();

        // The platform catalog includes the module catalogs; the Identity catalog is registered by
        // AddIdentityApplication.
        services.AddSingleton<IPermissionCatalogSource, CrmPermissionCatalog>();
        services.AddSingleton<IPermissionCatalogSource, SalesPermissionCatalog>();

        // The parity suite drives persistence directly; the outbox recovery loop is not started.
        services.Configure<OutboxOptions>(outbox => outbox.ProcessingEnabled = false);

        ServiceProvider provider = services.BuildServiceProvider();
        return new PostgreSqlIdentityEnvironment(container, provider, connectionString);
    }
}
