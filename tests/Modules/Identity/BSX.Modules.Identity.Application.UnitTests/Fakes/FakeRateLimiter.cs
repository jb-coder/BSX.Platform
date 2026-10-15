using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

internal sealed class FakeRateLimiter : IRateLimiter
{
    private readonly int _maxRequests;
    private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

    public FakeRateLimiter(int maxRequests = int.MaxValue) => _maxRequests = maxRequests;

    public RateLimitDecision Acquire(string partitionKey, RateLimitPolicy policy)
    {
        _counts.TryGetValue(partitionKey, out int count);

        if (count >= _maxRequests)
        {
            return new RateLimitDecision(false, TimeSpan.FromMinutes(1));
        }

        _counts[partitionKey] = count + 1;
        return new RateLimitDecision(true, null);
    }
}
