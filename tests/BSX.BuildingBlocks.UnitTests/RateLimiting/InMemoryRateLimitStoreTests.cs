using BSX.BuildingBlocks.DependencyInjection;
using BSX.BuildingBlocks.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace BSX.BuildingBlocks.UnitTests.RateLimiting;

public sealed class InMemoryRateLimitStoreTests
{
    private static readonly RateLimitPolicy Policy = new("login", PermitLimit: 2, Window: TimeSpan.FromMinutes(1));

    [Fact]
    public async Task Should_AllowUpToThePolicyLimit_ThenDeny()
    {
        var time = new AdvanceableTimeProvider(DateTimeOffset.UtcNow);
        IRateLimitStore store = CreateStore(time);
        var request = new RateLimitRequest("user@example.com", Policy);

        (await store.AcquireAsync(request)).IsAllowed.Should().BeTrue();
        (await store.AcquireAsync(request)).IsAllowed.Should().BeTrue();

        RateLimitDecision denied = await store.AcquireAsync(request);

        denied.IsAllowed.Should().BeFalse();
        denied.Remaining.Should().Be(0);
        denied.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_Reset_AfterTheWindow()
    {
        var time = new AdvanceableTimeProvider(DateTimeOffset.UtcNow);
        IRateLimitStore store = CreateStore(time);
        var request = new RateLimitRequest("user@example.com", Policy);

        (await store.AcquireAsync(request)).IsAllowed.Should().BeTrue();
        (await store.AcquireAsync(request)).IsAllowed.Should().BeTrue();
        (await store.AcquireAsync(request)).IsAllowed.Should().BeFalse();

        time.Advance(Policy.Window + TimeSpan.FromSeconds(1));

        (await store.AcquireAsync(request)).IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Should_IsolatePartitions()
    {
        IRateLimitStore store = CreateStore(new AdvanceableTimeProvider(DateTimeOffset.UtcNow));

        (await store.AcquireAsync(new RateLimitRequest("first@example.com", Policy))).IsAllowed.Should().BeTrue();
        (await store.AcquireAsync(new RateLimitRequest("first@example.com", Policy))).IsAllowed.Should().BeTrue();
        (await store.AcquireAsync(new RateLimitRequest("first@example.com", Policy))).IsAllowed.Should().BeFalse();

        (await store.AcquireAsync(new RateLimitRequest("second@example.com", Policy))).IsAllowed.Should().BeTrue();
    }

    private static IRateLimitStore CreateStore(TimeProvider timeProvider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(timeProvider);
        services.AddBuildingBlocks();

        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IRateLimitStore>();
    }

    private sealed class AdvanceableTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public AdvanceableTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
