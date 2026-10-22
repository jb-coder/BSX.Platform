using BSX.Modules.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Modules.Sales.Endpoints.HealthChecks;

/// <summary>
/// Readiness check that detects Sales schema drift: the Sales database must have no pending EF Core
/// migrations. Registered only when <c>Sales:Database:VerifySchema</c> is enabled, so environments
/// that bootstrap the schema another way are unaffected.
/// </summary>
internal sealed class SalesSchemaHealthCheck : IHealthCheck
{
    private readonly SalesDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="SalesSchemaHealthCheck"/> class.</summary>
    /// <param name="context">The Sales database context.</param>
    public SalesSchemaHealthCheck(SalesDbContext context) => _context = context;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<string> pending = (await _context.Database
                .GetPendingMigrationsAsync(cancellationToken)
                .ConfigureAwait(false))
                .ToList();

            return pending.Count == 0
                ? HealthCheckResult.Healthy("The Sales schema is up to date.")
                : HealthCheckResult.Unhealthy($"The Sales schema has {pending.Count} pending migration(s).");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The Sales schema could not be verified.", exception);
        }
    }
}
