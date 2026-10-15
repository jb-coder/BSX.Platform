namespace BSX.Web.IntegrationTests;

/// <summary>
/// A controllable <see cref="TimeProvider"/> used to exercise time-dependent Identity flows
/// (for example password-reset token expiry) deterministically from an endpoint test.
/// </summary>
public sealed class MutableTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    /// <summary>Initializes a new instance of the <see cref="MutableTimeProvider"/> class.</summary>
    /// <param name="now">The initial UTC time.</param>
    public MutableTimeProvider(DateTimeOffset now) => _now = now;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _now;

    /// <summary>Sets the current UTC time.</summary>
    /// <param name="now">The new current UTC time.</param>
    public void Set(DateTimeOffset now) => _now = now;

    /// <summary>Advances the current UTC time by the given delta.</summary>
    /// <param name="delta">The amount to advance.</param>
    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}
