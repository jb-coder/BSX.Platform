namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>Strongly typed identifier for an opportunity.</summary>
public sealed class OpportunityId : SalesId
{
    private OpportunityId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static OpportunityId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static OpportunityId From(Guid value) => new(value);
}
