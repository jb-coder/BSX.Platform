using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.Users.Credentials;

/// <summary>A credential linked from an external identity provider.</summary>
public sealed class ExternalProviderCredential : Credential
{
    internal ExternalProviderCredential(
        CredentialId id,
        ExternalProvider provider,
        string subjectId,
        DateTimeOffset createdOnUtc)
        : base(id, createdOnUtc)
    {
        Provider = provider;
        SubjectId = subjectId;
    }

    private ExternalProviderCredential()
    {
    }

    /// <inheritdoc />
    public override CredentialType Type => CredentialType.ExternalProvider;

    /// <summary>Gets the external provider.</summary>
    public ExternalProvider Provider { get; private set; }

    /// <summary>Gets the subject identifier issued by the provider.</summary>
    public string SubjectId { get; private set; } = null!;
}
