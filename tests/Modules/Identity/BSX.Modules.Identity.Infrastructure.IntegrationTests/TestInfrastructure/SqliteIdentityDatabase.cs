using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Identity.Infrastructure.Persistence;
using BSX.Modules.Identity.Infrastructure.Persistence.Interceptors;
using BSX.Modules.Identity.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;

/// <summary>
/// A self-contained SQLite (relational) database for integration tests. Production uses
/// PostgreSQL; SQLite validates relational behavior (filters, TPH, owned types, interceptors)
/// without an external dependency.
/// </summary>
internal sealed class SqliteIdentityDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteIdentityDatabase(SqliteConnection connection, IdentityDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    internal IdentityDbContext Context { get; }

    internal static async Task<SqliteIdentityDatabase> CreateAsync(ICurrentUser? currentUser = null)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new AuditingSaveChangesInterceptor(TimeProvider.System, currentUser ?? new SystemCurrentUser()))
            .Options;

        var context = new IdentityDbContext(options);
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        return new SqliteIdentityDatabase(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
