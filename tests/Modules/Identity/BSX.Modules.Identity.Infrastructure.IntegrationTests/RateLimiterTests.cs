using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Infrastructure.IntegrationTests.TestInfrastructure;
using BSX.Modules.Identity.Infrastructure.Security;

namespace BSX.Modules.Identity.Infrastructure.IntegrationTests;

public sealed class RateLimiterTests
{
    [Fact]
    public void Should_AllowUpToThreshold_ThenDeny()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.UnixEpoch);
        var limiter = new InMemoryRateLimiter(timeProvider);
        var policy = new RateLimitPolicy(MaxRequests: 2, Window: TimeSpan.FromMinutes(1));

        limiter.Acquire("login:user@example.com", policy).IsAllowed.Should().BeTrue();
        limiter.Acquire("login:user@example.com", policy).IsAllowed.Should().BeTrue();

        RateLimitDecision denied = limiter.Acquire("login:user@example.com", policy);

        denied.IsAllowed.Should().BeFalse();
        denied.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public void Should_ResetAfterWindow()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.UnixEpoch);
        var limiter = new InMemoryRateLimiter(timeProvider);
        var policy = new RateLimitPolicy(MaxRequests: 1, Window: TimeSpan.FromMinutes(1));

        limiter.Acquire("refresh:abc", policy).IsAllowed.Should().BeTrue();
        limiter.Acquire("refresh:abc", policy).IsAllowed.Should().BeFalse();

        timeProvider.UtcNow = timeProvider.UtcNow.AddMinutes(2);

        limiter.Acquire("refresh:abc", policy).IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void Should_Partition_ByKey()
    {
        var timeProvider = new MutableTimeProvider(DateTimeOffset.UnixEpoch);
        var limiter = new InMemoryRateLimiter(timeProvider);
        var policy = new RateLimitPolicy(MaxRequests: 1, Window: TimeSpan.FromMinutes(1));

        limiter.Acquire("login:a@example.com", policy).IsAllowed.Should().BeTrue();

        limiter.Acquire("login:b@example.com", policy).IsAllowed.Should().BeTrue();
    }
}
