namespace BSX.BuildingBlocks.RateLimiting;

/// <summary>
/// Platform rate limiting seam. The in-process implementation is accurate for a single instance;
/// high-availability deployments replace it with a shared implementation (for example Redis) that
/// implements <see cref="IDistributedRateLimitStore"/> without changing callers.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>Attempts to acquire a permit for the request.</summary>
    /// <param name="request">The partition and policy.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The acquisition decision.</returns>
    ValueTask<RateLimitDecision> AcquireAsync(
        RateLimitRequest request,
        CancellationToken cancellationToken = default);
}
