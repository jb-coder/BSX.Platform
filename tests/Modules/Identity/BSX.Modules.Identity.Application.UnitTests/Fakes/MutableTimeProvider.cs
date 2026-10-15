namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

internal sealed class MutableTimeProvider : TimeProvider
{
    public MutableTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
