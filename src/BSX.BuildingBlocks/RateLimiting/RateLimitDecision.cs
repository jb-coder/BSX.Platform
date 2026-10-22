namespace BSX.BuildingBlocks.RateLimiting;

/// <summary>The outcome of a rate limit acquisition.</summary>
/// <param name="IsAllowed">Whether the request may proceed.</param>
/// <param name="Limit">The configured permit limit.</param>
/// <param name="Remaining">The permits remaining in the current window.</param>
/// <param name="RetryAfter">How long the caller should wait before retrying, when denied.</param>
/// <param name="ResetAtUtc">When the current window resets.</param>
public sealed record RateLimitDecision(
    bool IsAllowed,
    int Limit,
    int Remaining,
    TimeSpan RetryAfter,
    DateTimeOffset ResetAtUtc);
