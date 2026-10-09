namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a tenant membership.</summary>
public sealed class MembershipId : IdentityId
{
    private MembershipId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static MembershipId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static MembershipId From(Guid value) => new(value);
}
