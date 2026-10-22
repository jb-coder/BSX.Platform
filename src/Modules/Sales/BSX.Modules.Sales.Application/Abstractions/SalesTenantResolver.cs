using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Sales.Application.Errors;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Sales.Application.Abstractions;

/// <summary>
/// Resolves the active tenant from <see cref="ITenantContext"/> (ADR-013). Client-supplied tenant
/// values are never accepted: the tenant always comes from the authenticated request context.
/// </summary>
public sealed class SalesTenantResolver
{
    private readonly ITenantContext _tenantContext;

    /// <summary>Initializes a new instance of the <see cref="SalesTenantResolver"/> class.</summary>
    /// <param name="tenantContext">The tenant context.</param>
    public SalesTenantResolver(ITenantContext tenantContext) => _tenantContext = tenantContext;

    /// <summary>Resolves the active tenant identifier.</summary>
    public Result<TenantId> Resolve()
    {
        if (_tenantContext.TenantId is not { } tenantId || tenantId == Guid.Empty)
        {
            return Result.Failure<TenantId>(SalesApplicationErrors.TenantRequired);
        }

        return Result.Success(TenantId.From(tenantId));
    }
}
