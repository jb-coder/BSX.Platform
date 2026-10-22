using BSX.Modules.Sales.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Modules.Sales.Endpoints.HealthChecks;

/// <summary>Registers the Sales readiness health checks.</summary>
public static class SalesHealthChecksExtensions
{
    /// <summary>Adds the Sales database readiness check (tag <c>ready</c>).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSalesHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddCheck<SalesDatabaseHealthCheck>("sales-database", tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Adds the Sales schema readiness check that detects pending EF Core migrations. Enable it with
    /// <c>Sales:Database:VerifySchema</c> in deployments that own the schema.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSalesSchemaHealthCheck(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddCheck<SalesSchemaHealthCheck>("sales-database-schema", tags: ["ready"]);

        return services;
    }
}

/// <summary>Verifies that the Sales database is reachable.</summary>
internal sealed class SalesDatabaseHealthCheck : IHealthCheck
{
    private readonly SalesDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="SalesDatabaseHealthCheck"/> class.</summary>
    /// <param name="context">The Sales database context.</param>
    public SalesDatabaseHealthCheck(SalesDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
        => await _context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The Sales database is unreachable.");
}
