namespace BSX.Web.Authentication;

/// <summary>
/// Controls how often authorization is revalidated (ADR-021). Intervals are expressed in seconds so
/// they can be tuned per deployment without code changes.
/// </summary>
public sealed class AuthorizationRevalidationOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "AuthorizationRevalidation";

    /// <summary>
    /// The interval between circuit revalidation attempts. Defaults to 30 seconds.
    /// </summary>
    public int CircuitIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// The maximum age of the cookie principal before the next HTTP request revalidates it.
    /// Defaults to 30 seconds. Set to 0 to revalidate on every request.
    /// </summary>
    public int CookieIntervalSeconds { get; set; } = 30;

    /// <summary>Gets the circuit revalidation interval.</summary>
    public TimeSpan CircuitInterval => TimeSpan.FromSeconds(Math.Max(1, CircuitIntervalSeconds));

    /// <summary>Gets the cookie revalidation interval.</summary>
    public TimeSpan CookieInterval => TimeSpan.FromSeconds(Math.Max(0, CookieIntervalSeconds));
}
