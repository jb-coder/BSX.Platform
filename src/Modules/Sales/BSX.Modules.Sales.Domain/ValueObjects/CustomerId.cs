namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>
/// Integration reference to a CRM customer. Sales never references the CRM aggregate; it stores the
/// identifier only (module-boundaries, ADR-003/ADR-013).
/// </summary>
public sealed class CustomerId : SalesId
{
    private CustomerId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a reference from an existing CRM customer identifier.</summary>
    /// <param name="value">The customer identifier value.</param>
    public static CustomerId From(Guid value) => new(value);
}
