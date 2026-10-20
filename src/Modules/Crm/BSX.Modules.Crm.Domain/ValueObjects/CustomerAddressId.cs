namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>Strongly typed identifier for an address owned by a customer.</summary>
public sealed class CustomerAddressId : CrmId
{
    private CustomerAddressId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static CustomerAddressId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static CustomerAddressId From(Guid value) => new(value);
}
