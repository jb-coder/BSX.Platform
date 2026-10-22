using System.Collections.Concurrent;

namespace BSX.BuildingBlocks.RateLimiting;

/// <summary>
/// Single-instance fixed-window rate limiter. Limits are per process and reset on restart, which
/// is documented and acceptable for fail-closed authentication endpoints (see
/// <c>docs/operations/rate-limiting.md</c>).
/// </summary>
internal sealed class InMemoryRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, Window> _windows = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="InMemoryRateLimitStore"/> class.</summary>
    /// <param name="timeProvider">The time provider.</param>
    public InMemoryRateLimitStore(TimeProvider timeProvider) => _timeProvider = timeProvider;

    /// <inheritdoc />
    public ValueTask<RateLimitDecision> AcquireAsync(
        RateLimitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        string key = $"{request.Policy.Name}:{request.PartitionKey}";
        Window window = _windows.GetOrAdd(key, _ => new Window(now.Add(request.Policy.Window)));

        lock (window)
        {
            if (now >= window.ResetAtUtc)
            {
                window.Reset(now.Add(request.Policy.Window));
            }

            if (window.Count >= request.Policy.PermitLimit)
            {
                TimeSpan retryAfter = window.ResetAtUtc - now;
                return ValueTask.FromResult(new RateLimitDecision(
                    IsAllowed: false,
                    request.Policy.PermitLimit,
                    Remaining: 0,
                    retryAfter < TimeSpan.Zero ? TimeSpan.Zero : retryAfter,
                    window.ResetAtUtc));
            }

            window.Increment();
            return ValueTask.FromResult(new RateLimitDecision(
                IsAllowed: true,
                request.Policy.PermitLimit,
                request.Policy.PermitLimit - window.Count,
                TimeSpan.Zero,
                window.ResetAtUtc));
        }
    }

    private sealed class Window
    {
        public Window(DateTimeOffset resetAtUtc) => ResetAtUtc = resetAtUtc;

        public int Count { get; private set; }

        public DateTimeOffset ResetAtUtc { get; private set; }

        public void Increment() => Count++;

        public void Reset(DateTimeOffset resetAtUtc)
        {
            Count = 0;
            ResetAtUtc = resetAtUtc;
        }
    }
}
