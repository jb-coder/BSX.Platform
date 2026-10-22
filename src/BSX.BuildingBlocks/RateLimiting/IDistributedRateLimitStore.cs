namespace BSX.BuildingBlocks.RateLimiting;

/// <summary>
/// Marker for a cluster-wide rate limit store. Replacing the default
/// <see cref="IRateLimitStore"/> with an implementation of this interface makes limits accurate
/// across all instances without any caller changes.
/// </summary>
public interface IDistributedRateLimitStore : IRateLimitStore
{
}
