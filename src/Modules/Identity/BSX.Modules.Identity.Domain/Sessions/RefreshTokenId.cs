namespace BSX.Modules.Identity.Domain.Sessions;

/// <summary>Strongly typed identifier for a refresh token.</summary>
public sealed class RefreshTokenId : ValueObjects.IdentityId
{
    private RefreshTokenId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static RefreshTokenId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static RefreshTokenId From(Guid value) => new(value);
}
