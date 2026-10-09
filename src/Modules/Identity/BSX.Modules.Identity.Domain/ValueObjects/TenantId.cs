namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a tenant.</summary>
public sealed class TenantId : IdentityId
{
    private TenantId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static TenantId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static TenantId From(Guid value) => new(value);
}
