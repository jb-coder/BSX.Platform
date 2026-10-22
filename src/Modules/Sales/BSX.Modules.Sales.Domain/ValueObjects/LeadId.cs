namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>Strongly typed identifier for a lead.</summary>
public sealed class LeadId : SalesId
{
    private LeadId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static LeadId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static LeadId From(Guid value) => new(value);
}
