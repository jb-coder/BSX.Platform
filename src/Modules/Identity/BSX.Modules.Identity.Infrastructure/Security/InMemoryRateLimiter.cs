using System.Collections.Concurrent;
using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>
/// In-process fixed-window rate limiter. Suitable for a single instance; replace with a
/// distributed limiter (for example Redis) when the API scales out.
/// </summary>
internal sealed class InMemoryRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, WindowCounter> _counters = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="InMemoryRateLimiter"/> class.</summary>
    /// <param name="timeProvider">The time source.</param>
    public InMemoryRateLimiter(TimeProvider timeProvider) => _timeProvider = timeProvider;

    /// <inheritdoc />
    public RateLimitDecision Acquire(string partitionKey, RateLimitPolicy policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        ArgumentNullException.ThrowIfNull(policy);

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        WindowCounter counter = _counters.GetOrAdd(partitionKey, _ => new WindowCounter(nowUtc));

        lock (counter)
        {
            if (nowUtc - counter.WindowStart >= policy.Window)
            {
                counter.WindowStart = nowUtc;
                counter.Count = 0;
            }

            if (counter.Count >= policy.MaxRequests)
            {
                TimeSpan retryAfter = counter.WindowStart + policy.Window - nowUtc;
                return new RateLimitDecision(false, retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero);
            }

            counter.Count++;
            return new RateLimitDecision(true, null);
        }
    }

    private sealed class WindowCounter
    {
        public WindowCounter(DateTimeOffset windowStart) => WindowStart = windowStart;

        public int Count { get; set; }

        public DateTimeOffset WindowStart { get; set; }
    }
}
