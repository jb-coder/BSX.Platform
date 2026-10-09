using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.Users.Credentials;

/// <summary>A password credential.</summary>
public sealed class PasswordCredential : Credential
{
    internal PasswordCredential(CredentialId id, PasswordHash hash, DateTimeOffset createdOnUtc)
        : base(id, createdOnUtc)
        => Hash = hash;

    private PasswordCredential()
    {
    }

    /// <inheritdoc />
    public override CredentialType Type => CredentialType.Password;

    /// <summary>Gets the stored password hash.</summary>
    public PasswordHash Hash { get; private set; } = null!;

    internal void Update(PasswordHash hash) => Hash = hash;
}
