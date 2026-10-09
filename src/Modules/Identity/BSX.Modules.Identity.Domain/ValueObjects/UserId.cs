namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a user.</summary>
public sealed class UserId : IdentityId
{
    private UserId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static UserId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static UserId From(Guid value) => new(value);
}
