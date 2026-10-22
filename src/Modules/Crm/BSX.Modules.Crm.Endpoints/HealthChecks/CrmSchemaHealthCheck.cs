using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Modules.Crm.Endpoints.HealthChecks;

/// <summary>
/// Readiness check that detects CRM schema drift: the CRM database must have no pending EF Core
/// migrations. Registered only when <c>Crm:Database:VerifySchema</c> is enabled, so environments
/// that bootstrap the schema another way are unaffected.
/// </summary>
internal sealed class CrmSchemaHealthCheck : IHealthCheck
{
    private readonly CrmDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CrmSchemaHealthCheck"/> class.</summary>
    /// <param name="context">The CRM database context.</param>
    public CrmSchemaHealthCheck(CrmDbContext context) => _context = context;

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
                ? HealthCheckResult.Healthy("The CRM schema is up to date.")
                : HealthCheckResult.Unhealthy($"The CRM schema has {pending.Count} pending migration(s).");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The CRM schema could not be verified.", exception);
        }
    }
}
