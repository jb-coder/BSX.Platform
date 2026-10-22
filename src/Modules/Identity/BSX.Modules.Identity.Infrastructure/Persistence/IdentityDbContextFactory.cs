using BSX.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BSX.Modules.Identity.Infrastructure.Persistence;

/// <summary>Design-time factory used by the EF Core tools to create migrations.</summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    /// <inheritdoc />
    public IdentityDbContext CreateDbContext(string[] args)
    {
        string connectionString = DesignTimeConnection.Resolve(
            args,
            "Identity",
            "Host=localhost;Port=5432;Database=bsx_identity;Username=postgres;Password=postgres");

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
            .Options;

        return new IdentityDbContext(options);
    }
}
