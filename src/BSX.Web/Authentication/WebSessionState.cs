namespace BSX.Web.Authentication;

/// <summary>
/// Circuit-scoped state used to render a friendly session-expired experience when the revalidating
/// authentication state provider signs the circuit out (ADR-021).
/// </summary>
public sealed class WebSessionState
{
    /// <summary>Gets or sets a value indicating whether the session was terminated by revalidation.</summary>
    public bool Expired { get; set; }

    /// <summary>Gets or sets the non-sensitive reason used for diagnostics.</summary>
    public string? Reason { get; set; }
}
