using BSX.Modules.Identity.Infrastructure.Jwt;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Modules.Identity.Endpoints.HealthChecks;

/// <summary>Registers Identity health checks (liveness, database and authentication readiness).</summary>
public static class IdentityHealthChecksExtensions
{
    /// <summary>Adds the identity liveness and readiness health checks.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddIdentityHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddCheck("self", static () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<IdentityDatabaseHealthCheck>("identity-database", tags: ["ready"])
            .AddCheck<IdentityAuthenticationHealthCheck>("identity-authentication", tags: ["ready"]);

        return services;
    }

    /// <summary>
    /// Adds the identity schema readiness check that detects pending EF Core migrations.
    /// Enable it with <c>Identity:Database:VerifySchema</c> in deployments that own the schema.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddIdentitySchemaHealthCheck(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddCheck<IdentitySchemaHealthCheck>("identity-database-schema", tags: ["ready"]);

        return services;
    }
}

/// <summary>Verifies that the identity database is reachable.</summary>
internal sealed class IdentityDatabaseHealthCheck : IHealthCheck
{
    private readonly IdentityDbContext _context;

    public IdentityDatabaseHealthCheck(IdentityDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => await _context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The identity database is unreachable.");
}

/// <summary>Verifies that the JWT signing keys are configured.</summary>
internal sealed class IdentityAuthenticationHealthCheck : IHealthCheck
{
    private readonly JwtKeyRing _keyRing;

    public IdentityAuthenticationHealthCheck(JwtKeyRing keyRing) => _keyRing = keyRing;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(_keyRing.GetValidationKeys().Count > 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("No JWT signing keys are configured."));
}
