namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a role.</summary>
public sealed class RoleId : IdentityId
{
    private RoleId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static RoleId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static RoleId From(Guid value) => new(value);
}
