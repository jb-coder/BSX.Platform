
namespace BSX.Web.Deployment;

/// <summary>
/// Reverse-proxy deployment settings. Forwarded headers are trusted only from the configured
/// proxies/networks; when none are configured the framework's loopback-only defaults apply.
/// </summary>
public sealed class ReverseProxyOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "ReverseProxy";

    /// <summary>Gets or sets a value indicating whether forwarded headers are processed.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the application redirects HTTP requests to HTTPS.
    /// Disable this only when the reverse proxy already performs the redirect.
    /// </summary>
    public bool RedirectHttpToHttps { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of proxies in front of the application. Increase it when more than
    /// one trusted proxy appends forwarded headers (for example an ingress plus a sidecar).
    /// </summary>
    public int ForwardLimit { get; set; } = 1;

    /// <summary>Gets or sets the trusted proxy IP addresses.</summary>
    public string[] KnownProxies { get; set; } = [];

    /// <summary>Gets or sets the trusted proxy networks in CIDR notation (for example <c>10.0.0.0/8</c>).</summary>
    public string[] KnownNetworks { get; set; } = [];
}

/// <summary>Data Protection key-ring settings.</summary>
public sealed class DataProtectionOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "DataProtection";

    /// <summary>
    /// Gets or sets the application name used to isolate the key ring. Every instance that shares
    /// authentication cookies must use the same value.
    /// </summary>
    public string ApplicationName { get; set; } = "BSX.Platform";

    /// <summary>
    /// Gets or sets the directory that stores the key ring. Outside Development this is required so
    /// cookies and antiforgery tokens survive restarts and are readable by every instance.
    /// </summary>
    public string? KeysPath { get; set; }
}

/// <summary>Cookie security settings shared by authentication, refresh-token and antiforgery cookies.</summary>
public sealed class WebCookieOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Web:Cookies";

    /// <summary>
    /// Gets or sets the secure-cookie policy. The production-safe default is
    /// <see cref="CookieSecurePolicy.Always"/>; Development overrides it to
    /// <see cref="CookieSecurePolicy.SameAsRequest"/> when the section is absent so the HTTP
    /// launch profile keeps working.
    /// </summary>
    public CookieSecurePolicy SecurePolicy { get; set; } = CookieSecurePolicy.Always;

    /// <summary>Gets or sets the authentication cookie SameSite policy.</summary>
    public SameSiteMode AuthSameSite { get; set; } = SameSiteMode.Lax;

    /// <summary>Gets or sets the refresh-token cookie SameSite policy.</summary>
    public SameSiteMode RefreshSameSite { get; set; } = SameSiteMode.Strict;

    /// <summary>Gets or sets the antiforgery cookie SameSite policy.</summary>
    public SameSiteMode AntiforgerySameSite { get; set; } = SameSiteMode.Strict;

    /// <summary>Gets or sets an optional cookie domain. Leave empty for host-only cookies.</summary>
    public string? Domain { get; set; }
}
