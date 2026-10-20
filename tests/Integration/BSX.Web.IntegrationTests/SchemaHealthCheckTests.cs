using BSX.Modules.Identity.Endpoints.HealthChecks;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Web.IntegrationTests;

/// <summary>Schema readiness detection (D2): pending migrations must fail the readiness check.</summary>
public sealed class SchemaHealthCheckTests
{
    [Fact]
    public async Task Should_BeUnhealthy_WhenMigrationsArePending()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new IdentityDbContext(options);
        var check = new IdentitySchemaHealthCheck(context);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
