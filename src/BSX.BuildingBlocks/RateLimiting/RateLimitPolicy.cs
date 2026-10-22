namespace BSX.BuildingBlocks.RateLimiting;

/// <summary>Defines a named fixed-window rate limit.</summary>
/// <param name="Name">The stable policy name used as part of the partition key.</param>
/// <param name="PermitLimit">The number of permits allowed per window.</param>
/// <param name="Window">The window length.</param>
public sealed record RateLimitPolicy(string Name, int PermitLimit, TimeSpan Window);
