namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a customer.</summary>
public sealed class CustomerId : CrmId
{
    private CustomerId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static CustomerId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static CustomerId From(Guid value) => new(value);
}
