namespace BSX.Modules.Identity.Domain.Enums;

/// <summary>
/// Supported external identity providers.
/// </summary>
public enum ExternalProvider
{
    /// <summary>A generic OpenID Connect provider.</summary>
    OpenIdConnect = 0,

    /// <summary>Microsoft identity platform.</summary>
    Microsoft = 1,

    /// <summary>Google identity.</summary>
    Google = 2,

    /// <summary>Apple identity.</summary>
    Apple = 3,
}
