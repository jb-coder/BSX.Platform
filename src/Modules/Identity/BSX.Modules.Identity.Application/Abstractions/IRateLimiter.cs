namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>A fixed-window rate-limit policy.</summary>
/// <param name="MaxRequests">The maximum number of requests allowed within the window.</param>
/// <param name="Window">The window duration.</param>
public sealed record RateLimitPolicy(int MaxRequests, TimeSpan Window);

/// <summary>The outcome of a rate-limit acquisition.</summary>
/// <param name="IsAllowed">Whether the request is allowed.</param>
/// <param name="RetryAfter">The retry-after hint when the request is denied.</param>
public sealed record RateLimitDecision(bool IsAllowed, TimeSpan? RetryAfter);

/// <summary>
/// Limits the rate of sensitive authentication operations (ADR-017). Implemented in Infrastructure.
/// Must be fail-closed: deny when the limit is exceeded.
/// </summary>
public interface IRateLimiter
{
    /// <summary>Attempts to acquire a slot for the given partition key.</summary>
    /// <param name="partitionKey">The partition key (for example a normalized email or token hash).</param>
    /// <param name="policy">The rate-limit policy.</param>
    RateLimitDecision Acquire(string partitionKey, RateLimitPolicy policy);
}
