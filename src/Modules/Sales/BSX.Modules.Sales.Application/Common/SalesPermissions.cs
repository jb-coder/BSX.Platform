namespace BSX.Modules.Sales.Application.Common;

/// <summary>Stable permission codes for the Sales module (ADR-012).</summary>
public static class SalesPermissions
{
    /// <summary>Read leads.</summary>
    public const string LeadsRead = "sales.leads.read";

    /// <summary>Create leads.</summary>
    public const string LeadsCreate = "sales.leads.create";

    /// <summary>Update leads and their lifecycle.</summary>
    public const string LeadsUpdate = "sales.leads.update";

    /// <summary>Read opportunities.</summary>
    public const string OpportunitiesRead = "sales.opportunities.read";

    /// <summary>Create opportunities from qualified leads.</summary>
    public const string OpportunitiesCreate = "sales.opportunities.create";

    /// <summary>Update opportunities and their lifecycle.</summary>
    public const string OpportunitiesUpdate = "sales.opportunities.update";
}
