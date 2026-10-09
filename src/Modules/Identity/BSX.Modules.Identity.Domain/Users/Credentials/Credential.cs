using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.Users.Credentials;

/// <summary>
/// Base class for human authentication credentials owned by the <see cref="User"/> aggregate
/// (ADR-014).
/// </summary>
public abstract class Credential : Entity<CredentialId>
{
    /// <summary>Initializes a new instance of the <see cref="Credential"/> class.</summary>
    /// <param name="id">The credential identifier.</param>
    /// <param name="createdOnUtc">Creation timestamp.</param>
    protected Credential(CredentialId id, DateTimeOffset createdOnUtc)
        : base(id)
        => CreatedOnUtc = createdOnUtc;

    /// <summary>Initializes a new instance of the <see cref="Credential"/> class for persistence.</summary>
    protected Credential()
    {
    }

    /// <summary>Gets the credential kind.</summary>
    public abstract CredentialType Type { get; }

    /// <summary>Gets the creation timestamp.</summary>
    public DateTimeOffset CreatedOnUtc { get; private set; }
}
