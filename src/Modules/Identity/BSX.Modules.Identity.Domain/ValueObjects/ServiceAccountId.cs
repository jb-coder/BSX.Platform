namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a service account.</summary>
public sealed class ServiceAccountId : IdentityId
{
    private ServiceAccountId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static ServiceAccountId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static ServiceAccountId From(Guid value) => new(value);
}
