using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.Users.Credentials;

/// <summary>A WebAuthn (passkey) credential.</summary>
public sealed class WebAuthnCredential : Credential
{
    internal WebAuthnCredential(
        CredentialId id,
        string credentialIdentifier,
        byte[] publicKey,
        uint signCount,
        string? name,
        DateTimeOffset createdOnUtc)
        : base(id, createdOnUtc)
    {
        CredentialIdentifier = credentialIdentifier;
        PublicKey = publicKey;
        SignCount = signCount;
        Name = name;
    }

    private WebAuthnCredential()
    {
    }

    /// <inheritdoc />
    public override CredentialType Type => CredentialType.WebAuthn;

    /// <summary>Gets the authenticator credential identifier.</summary>
    public string CredentialIdentifier { get; private set; } = null!;

    /// <summary>Gets the public key.</summary>
    public byte[] PublicKey { get; private set; } = [];

    /// <summary>Gets the signature counter used to detect cloned authenticators.</summary>
    public uint SignCount { get; private set; }

    /// <summary>Gets an optional operator label.</summary>
    public string? Name { get; private set; }

    /// <summary>Updates the signature counter after a successful assertion.</summary>
    /// <param name="signCount">The new counter value.</param>
    public void UpdateSignCount(uint signCount) => SignCount = signCount;
}
