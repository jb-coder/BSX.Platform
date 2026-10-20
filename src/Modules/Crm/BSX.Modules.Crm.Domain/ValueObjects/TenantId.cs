namespace BSX.Modules.Crm.Domain.ValueObjects;

/// <summary>
/// Strongly typed tenant identifier reserved for multi-tenancy (ADR-013).
/// CRM does not implement tenancy yet; aggregates carry the reserved identifier so isolation can
/// be added by the Infrastructure layer without remodeling.
/// </summary>
public sealed class TenantId : CrmId
{
    private TenantId(Guid value)
        : base(value)
    {
    }

    /// <summary>Creates a new identifier.</summary>
    public static TenantId New() => new(Guid.NewGuid());

    /// <summary>Creates an identifier from an existing value.</summary>
    /// <param name="value">The identifier value.</param>
    public static TenantId From(Guid value) => new(value);
}
