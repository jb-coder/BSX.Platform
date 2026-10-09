namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>
/// Principal identifier shared by users and service accounts for authorization and auditing
/// (ADR-014).
/// </summary>
public sealed class PrincipalId : IdentityId
{
    private PrincipalId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static PrincipalId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static PrincipalId From(Guid value) => new(value);
}
