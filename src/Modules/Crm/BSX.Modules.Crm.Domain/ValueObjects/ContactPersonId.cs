namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a contact person owned by a customer.</summary>
public sealed class ContactPersonId : CrmId
{
    private ContactPersonId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static ContactPersonId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static ContactPersonId From(Guid value) => new(value);
}
