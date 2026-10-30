namespace BSX.Web.Deployment;

/// <summary>Security response-header settings.</summary>
public sealed class SecurityHeadersOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "SecurityHeaders";

    /// <summary>
    /// The default Content Security Policy. It permits the framework's inline import map and
    /// WebSocket circuit while keeping the application self-contained apart from the configured
    /// Google Fonts stylesheet and font host.
    /// </summary>
    public const string DefaultContentSecurityPolicy =
        "default-src 'self'; "
        + "script-src 'self' 'unsafe-inline'; "
        + "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; "
        + "font-src 'self' https://fonts.gstatic.com data:; "
        + "img-src 'self' data:; "
        + "connect-src 'self' ws: wss:; "
        + "frame-ancestors 'none'; "
        + "base-uri 'self'; "
        + "form-action 'self'; "
        + "object-src 'none'";

    /// <summary>Gets or sets the Content Security Policy; set to null to disable it.</summary>
    public string? ContentSecurityPolicy { get; set; } = DefaultContentSecurityPolicy;

    /// <summary>Gets or sets the referrer policy.</summary>
    public string ReferrerPolicy { get; set; } = "no-referrer";

    /// <summary>Gets or sets the permissions policy.</summary>
    public string PermissionsPolicy { get; set; } = "camera=(), microphone=(), geolocation=()";

    /// <summary>Gets or sets a value indicating whether <c>X-Frame-Options: DENY</c> is emitted.</summary>
    public bool EnableFrameOptions { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the cross-origin opener policy is emitted.</summary>
    public bool EnableCrossOriginOpenerPolicy { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the cross-origin resource policy is emitted.</summary>
    public bool EnableCrossOriginResourcePolicy { get; set; } = true;
}
