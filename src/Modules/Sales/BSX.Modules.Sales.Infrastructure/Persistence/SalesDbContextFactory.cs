using BSX.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BSX.Modules.Sales.Infrastructure.Persistence;

/// <summary>Design-time factory used by the EF Core tools to create migrations.</summary>
public sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    /// <inheritdoc />
    public SalesDbContext CreateDbContext(string[] args)
    {
        string connectionString = DesignTimeConnection.Resolve(
            args,
            "Sales",
            "Host=localhost;Port=5432;Database=bsx_sales;Username=postgres;Password=postgres");

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "sales"))
            .Options;

        return new SalesDbContext(options);
    }
}
