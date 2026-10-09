namespace BSX.Modules.Identity.Domain.ValueObjects;

/// <summary>Strongly typed identifier for an API key.</summary>
public sealed class ApiKeyId : IdentityId
{
    private ApiKeyId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static ApiKeyId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    public static ApiKeyId From(Guid value) => new(value);
}
