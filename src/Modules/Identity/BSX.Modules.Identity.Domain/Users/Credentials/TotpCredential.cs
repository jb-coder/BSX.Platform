using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.Users.Credentials;

/// <summary>A time-based one-time password (TOTP) credential used for MFA.</summary>
public sealed class TotpCredential : Credential
{
    internal TotpCredential(CredentialId id, TotpSecret secret, DateTimeOffset createdOnUtc)
        : base(id, createdOnUtc)
        => Secret = secret;

    private TotpCredential()
    {
    }

    /// <inheritdoc />
    public override CredentialType Type => CredentialType.Totp;

    /// <summary>Gets the encrypted shared secret.</summary>
    public TotpSecret Secret { get; private set; } = null!;

    /// <summary>Gets the timestamp the factor was verified, if any.</summary>
    public DateTimeOffset? VerifiedOnUtc { get; private set; }

    internal void MarkVerified(DateTimeOffset nowUtc) => VerifiedOnUtc = nowUtc;
}
