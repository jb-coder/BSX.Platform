using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSX.Web.Deployment;

/// <summary>
/// Readiness check proving the Data Protection key ring can protect and unprotect a payload. Web
/// authentication cookies, refresh cookies and antiforgery tokens all depend on this key ring.
/// </summary>
internal sealed class DataProtectionHealthCheck : IHealthCheck
{
    private const string Purpose = "BSX.Deployment.Health";
    private readonly IDataProtectionProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="DataProtectionHealthCheck"/> class.</summary>
    /// <param name="provider">The Data Protection provider.</param>
    public DataProtectionHealthCheck(IDataProtectionProvider provider) => _provider = provider;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDataProtector protector = _provider.CreateProtector(Purpose);
            string payload = protector.Protect("ok");
            bool valid = string.Equals(protector.Unprotect(payload), "ok", StringComparison.Ordinal);

            return Task.FromResult(valid
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The Data Protection key ring returned an invalid payload."));
        }
        catch (Exception exception)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("The Data Protection key ring is unavailable.", exception));
        }
    }
}
