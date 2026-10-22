namespace BSX.Modules.Sales.Domain.ValueObjects;

/// <summary>
/// Integration reference to a CRM organization. Sales never references the CRM aggregate; it stores
/// the identifier only (module-boundaries, ADR-003/ADR-013).
/// </summary>
public sealed class OrganizationId : SalesId
{
    private OrganizationId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a reference from an existing CRM organization identifier.</summary>
    /// <param name="value">The organization identifier value.</param>
    public static OrganizationId From(Guid value) => new(value);
}
