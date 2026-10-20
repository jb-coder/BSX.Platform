namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a contact.</summary>
public sealed class ContactId : CrmId
{
    private ContactId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static ContactId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static ContactId From(Guid value) => new(value);
}
