namespace BSX.Modules.Crm.Infrastructure.Persistence;

/// <summary>
/// Resolves the tenant scope used by the CRM global query filters. The filters are fail-closed: a
/// missing tenant returns no data unless the scope is explicitly marked as a system scope.
/// </summary>
public interface ICrmTenantScope
{
    /// <summary>Gets the active tenant identifier, if any.</summary>
    Guid? TenantId { get; }

    /// <summary>Gets a value indicating whether this is an explicit privileged/system scope.</summary>
    bool IsSystem { get; }
}
