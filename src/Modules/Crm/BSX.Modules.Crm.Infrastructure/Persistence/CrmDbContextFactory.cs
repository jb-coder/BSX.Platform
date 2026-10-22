using BSX.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BSX.Modules.Crm.Infrastructure.Persistence;

/// <summary>Design-time factory used by the EF Core tools to create migrations.</summary>
public sealed class CrmDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
{
    /// <inheritdoc />
    public CrmDbContext CreateDbContext(string[] args)
    {
        string connectionString = DesignTimeConnection.Resolve(
            args,
            "Crm",
            "Host=localhost;Port=5432;Database=bsx_crm;Username=postgres;Password=postgres");

        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "crm"))
            .Options;

        return new CrmDbContext(options);
    }
}
