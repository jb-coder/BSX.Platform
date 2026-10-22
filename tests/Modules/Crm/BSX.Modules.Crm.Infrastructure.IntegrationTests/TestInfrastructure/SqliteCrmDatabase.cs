using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Crm.Infrastructure.Persistence;
using BSX.Modules.Crm.Infrastructure.Persistence.Interceptors;
using BSX.Persistence.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;

/// <summary>
/// A self-contained SQLite (relational) database for CRM integration tests. Production uses
/// PostgreSQL; SQLite validates relational behavior (indexes, filters, owned types, interceptors)
/// without an external dependency.
/// </summary>
internal sealed class SqliteCrmDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<CrmDbContext> _options;

    private SqliteCrmDatabase(SqliteConnection connection, DbContextOptions<CrmDbContext> options, CrmDbContext context)
    {
        _connection = connection;
        _options = options;
        Context = context;
    }

    internal CrmDbContext Context { get; }

    internal static async Task<SqliteCrmDatabase> CreateAsync(
        ICurrentUser? currentUser = null,
        TimeProvider? timeProvider = null,
        Guid? tenantId = null,
        bool isSystem = false,
        bool outbox = false)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var optionsBuilder = new DbContextOptionsBuilder<CrmDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new CrmAuditingSaveChangesInterceptor(
                timeProvider ?? TimeProvider.System,
                currentUser ?? new TestCurrentUser()));

        if (outbox)
        {
            optionsBuilder.AddInterceptors(new OutboxSaveChangesInterceptor(
                timeProvider ?? TimeProvider.System,
                Options.Create(new OutboxOptions())));
        }

        var options = optionsBuilder.Options;

        var context = new CrmDbContext(options, new TestCrmTenantScope(tenantId ?? TestData.TenantA, isSystem));
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        return new SqliteCrmDatabase(connection, options, context);
    }

    /// <summary>Creates another context over the same database (concurrency and tenant tests).</summary>
    internal CrmDbContext CreateContext(Guid? tenantId, bool isSystem = false)
        => new(_options, new TestCrmTenantScope(tenantId, isSystem));

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
