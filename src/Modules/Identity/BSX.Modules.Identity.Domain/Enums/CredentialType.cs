namespace BSX.Modules.Identity.Domain.Enums;

/// <summary>
/// The kind of an authentication credential.
/// </summary>
public enum CredentialType
{
    /// <summary>Password credential.</summary>
    Password = 0,

    /// <summary>Time-based one-time password (MFA).</summary>
    Totp = 1,

    /// <summary>One-time recovery codes (MFA).</summary>
    RecoveryCodes = 2,

    /// <summary>WebAuthn credential (passkey).</summary>
    WebAuthn = 3,

    /// <summary>External identity provider credential.</summary>
    ExternalProvider = 4,
}
