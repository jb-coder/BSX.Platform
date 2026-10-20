using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Crm.Application.Abstractions;

/// <summary>
/// Resolves the active tenant from <see cref="ITenantContext"/> (ADR-013). Client-supplied tenant
/// values are never accepted: the tenant always comes from the authenticated request context.
/// </summary>
public sealed class CrmTenantResolver
{
    private readonly ITenantContext _tenantContext;

    /// <summary>Initializes a new instance of the <see cref="CrmTenantResolver"/> class.</summary>
    /// <param name="tenantContext">The tenant context.</param>
    public CrmTenantResolver(ITenantContext tenantContext) => _tenantContext = tenantContext;

    /// <summary>Resolves the active tenant identifier.</summary>
    public Result<TenantId> Resolve()
    {
        if (_tenantContext.TenantId is not { } tenantId || tenantId == Guid.Empty)
        {
            return Result.Failure<TenantId>(CrmApplicationErrors.TenantRequired);
        }

        return Result.Success(TenantId.From(tenantId));
    }
}
