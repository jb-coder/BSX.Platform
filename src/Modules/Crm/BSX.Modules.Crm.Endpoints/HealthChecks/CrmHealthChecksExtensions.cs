using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Modules.Crm.Endpoints.HealthChecks;

/// <summary>Registers the CRM readiness health checks.</summary>
public static class CrmHealthChecksExtensions
{
    /// <summary>Adds the CRM database readiness check (tag <c>ready</c>).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCrmHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddCheck<CrmDatabaseHealthCheck>("crm-database", tags: ["ready"]);

        return services;
    }
}

/// <summary>Verifies that the CRM database is reachable.</summary>
internal sealed class CrmDatabaseHealthCheck : IHealthCheck
{
    private readonly CrmDbContext _context;

    public CrmDatabaseHealthCheck(CrmDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
        => await _context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The CRM database is unreachable.");
}
