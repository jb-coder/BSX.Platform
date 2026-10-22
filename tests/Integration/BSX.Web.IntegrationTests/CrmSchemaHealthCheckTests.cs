using BSX.Modules.Crm.Endpoints.HealthChecks;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Web.IntegrationTests;

/// <summary>CRM schema readiness detection: pending migrations must fail the readiness check.</summary>
public sealed class CrmSchemaHealthCheckTests
{
    [Fact]
    public async Task Should_BeUnhealthy_WhenMigrationsArePending()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new CrmDbContext(options);
        var check = new CrmSchemaHealthCheck(context);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
