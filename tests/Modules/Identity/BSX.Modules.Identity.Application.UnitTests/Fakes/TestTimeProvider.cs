namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>Fixed-time <see cref="TimeProvider"/> for deterministic handler tests.</summary>
internal sealed class TestTimeProvider : TimeProvider
{
    public TestTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; }

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
