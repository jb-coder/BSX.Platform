namespace BSX.Modules.Identity.Domain.Sessions;

/// <summary>Strongly typed identifier for a user session.</summary>
public sealed class SessionId : ValueObjects.IdentityId
{
    private SessionId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static SessionId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static SessionId From(Guid value) => new(value);
}
