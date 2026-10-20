using BSX.BuildingBlocks.Authorization;

namespace BSX.Modules.Crm.Infrastructure.Persistence;

/// <summary>
/// Default CRM tenant scope derived from <see cref="ITenantContext"/>. Background/operations code
/// may explicitly widen the scope on the scoped instance; request paths never do.
/// </summary>
public sealed class CrmTenantScope : ICrmTenantScope
{
    private readonly ITenantContext _tenantContext;
    private Guid? _tenantOverride;

    /// <summary>Initializes a new instance of the <see cref="CrmTenantScope"/> class.</summary>
    /// <param name="tenantContext">The ambient tenant context.</param>
    public CrmTenantScope(ITenantContext tenantContext) => _tenantContext = tenantContext;

    /// <inheritdoc />
    public Guid? TenantId => _tenantOverride ?? _tenantContext.TenantId;

    /// <inheritdoc />
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Explicitly scopes the unit of work to a tenant (background jobs). Request paths must not
    /// call this; they rely on the authenticated tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to operate on.</param>
    public void UseTenant(Guid tenantId)
    {
        _tenantOverride = tenantId;
        IsSystem = false;
    }

    /// <summary>
    /// Explicitly widens the scope to all tenants (privileged system/operations only). The
    /// soft-delete filter still applies.
    /// </summary>
    public void UseSystemScope() => IsSystem = true;
}
