namespace BSX.BuildingBlocks.RateLimiting;

/// <summary>A request to acquire a permit from a rate limit partition.</summary>
/// <param name="PartitionKey">The partition (for example, a normalized email or token hash).</param>
/// <param name="Policy">The policy to apply.</param>
public sealed record RateLimitRequest(string PartitionKey, RateLimitPolicy Policy);
