using BSX.Modules.Sales.Contracts.Permissions;

namespace BSX.Modules.Sales.Application.Common;

/// <summary>Stable permission codes for the Sales module (ADR-012).</summary>
public static class SalesPermissions
{
    /// <summary>Read leads.</summary>
    public const string LeadsRead = SalesPermissionCodes.LeadsRead;

    /// <summary>Create leads.</summary>
    public const string LeadsCreate = SalesPermissionCodes.LeadsCreate;

    /// <summary>Update leads and their lifecycle.</summary>
    public const string LeadsUpdate = SalesPermissionCodes.LeadsUpdate;

    /// <summary>Read opportunities.</summary>
    public const string OpportunitiesRead = SalesPermissionCodes.OpportunitiesRead;

    /// <summary>Create opportunities from qualified leads.</summary>
    public const string OpportunitiesCreate = SalesPermissionCodes.OpportunitiesCreate;

    /// <summary>Update opportunities and their lifecycle.</summary>
    public const string OpportunitiesUpdate = SalesPermissionCodes.OpportunitiesUpdate;
}
