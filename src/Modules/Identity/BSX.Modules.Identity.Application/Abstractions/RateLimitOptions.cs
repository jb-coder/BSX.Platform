namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Configurable rate-limit thresholds for authentication (ADR-017).</summary>
public sealed class RateLimitOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Identity:RateLimiting";

    /// <summary>Gets or sets the login policy.</summary>
    public RateLimitPolicy Login { get; set; } = new(MaxRequests: 5, Window: TimeSpan.FromMinutes(1));

    /// <summary>Gets or sets the refresh policy.</summary>
    public RateLimitPolicy Refresh { get; set; } = new(MaxRequests: 20, Window: TimeSpan.FromMinutes(1));
}
