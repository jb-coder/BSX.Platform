namespace BSX.Modules.Sales.Infrastructure.Persistence;

/// <summary>
/// Resolves the tenant scope used by the Sales global query filters. The filters are fail-closed: a
/// missing tenant returns no data unless the scope is explicitly marked as a system scope.
/// </summary>
public interface ISalesTenantScope
{
    /// <summary>Gets the active tenant identifier, if any.</summary>
    Guid? TenantId { get; }

    /// <summary>Gets a value indicating whether this is an explicit privileged/system scope.</summary>
    bool IsSystem { get; }
}
