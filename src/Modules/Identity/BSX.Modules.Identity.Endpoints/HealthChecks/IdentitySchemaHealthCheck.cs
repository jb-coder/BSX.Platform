using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Modules.Identity.Endpoints.HealthChecks;

/// <summary>
/// Readiness check that detects schema incompatibility: the identity database must have no
/// pending EF Core migrations. Registered only when <c>Identity:Database:VerifySchema</c> is
/// enabled so that environments bootstrapping the schema another way are unaffected.
/// </summary>
internal sealed class IdentitySchemaHealthCheck : IHealthCheck
{
    private readonly IdentityDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IdentitySchemaHealthCheck"/> class.</summary>
    /// <param name="context">The identity database context.</param>
    public IdentitySchemaHealthCheck(IdentityDbContext context) => _context = context;

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
                ? HealthCheckResult.Healthy("The identity schema is up to date.")
                : HealthCheckResult.Unhealthy($"The identity schema has {pending.Count} pending migration(s).");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The identity schema could not be verified.", exception);
        }
    }
}
