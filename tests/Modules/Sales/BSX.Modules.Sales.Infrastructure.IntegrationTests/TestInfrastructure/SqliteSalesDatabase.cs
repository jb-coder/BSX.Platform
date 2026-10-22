using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Sales.Infrastructure.Crm;
using BSX.Modules.Sales.Infrastructure.Persistence;
using BSX.Modules.Sales.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;

/// <summary>
/// A self-contained SQLite (relational) database for Sales integration tests. Production uses
/// PostgreSQL; SQLite validates relational behavior (indexes, filters, owned types, interceptors)
/// without an external dependency.
/// </summary>
internal sealed class SqliteSalesDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SalesDbContext> _options;

    private SqliteSalesDatabase(
        SqliteConnection connection,
        DbContextOptions<SalesDbContext> options,
        SalesDbContext context)
    {
        _connection = connection;
        _options = options;
        Context = context;
    }

    internal SalesDbContext Context { get; }

    internal static async Task<SqliteSalesDatabase> CreateAsync(
        ICurrentUser? currentUser = null,
        TimeProvider? timeProvider = null,
        Guid? tenantId = null,
        bool isSystem = false,
        ICrmReferenceValidator? crmReferenceValidator = null)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(
                new SalesAuditingSaveChangesInterceptor(
                    timeProvider ?? TimeProvider.System,
                    currentUser ?? new TestCurrentUser()),
                new CrmReferenceValidationInterceptor(
                    crmReferenceValidator ?? new TestCrmReferenceValidator()))
            .Options;

        var context = new SalesDbContext(options, new TestSalesTenantScope(tenantId ?? TestData.TenantA, isSystem));
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        return new SqliteSalesDatabase(connection, options, context);
    }

    /// <summary>Creates another context over the same database (concurrency and tenant tests).</summary>
    internal SalesDbContext CreateContext(Guid? tenantId, bool isSystem = false)
        => new(_options, new TestSalesTenantScope(tenantId, isSystem));

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
