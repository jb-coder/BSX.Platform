using BSX.Modules.Sales.Endpoints.HealthChecks;
using BSX.Modules.Sales.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Web.IntegrationTests;

/// <summary>Sales schema readiness detection: pending migrations must fail the readiness check.</summary>
public sealed class SalesSchemaHealthCheckTests
{
    [Fact]
    public async Task Should_BeUnhealthy_WhenMigrationsArePending()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SalesDbContext(options);
        var check = new SalesSchemaHealthCheck(context);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
