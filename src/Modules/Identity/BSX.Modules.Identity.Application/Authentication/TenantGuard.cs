using BSX.BuildingBlocks.Authorization;
using BSX.Modules.Identity.Application.Errors;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication;

/// <summary>
/// Enforces tenant isolation (ADR-013): an operation may only target the principal's active
/// tenant. Fail-closed — a missing or mismatched tenant is denied.
/// </summary>
public sealed class TenantGuard
{
    private readonly ITenantContext _tenantContext;

    /// <summary>Initializes a new instance of the <see cref="TenantGuard"/> class.</summary>
    /// <param name="tenantContext">The tenant context.</param>
    public TenantGuard(ITenantContext tenantContext) => _tenantContext = tenantContext;

    /// <summary>Ensures the target tenant matches the principal's active tenant.</summary>
    /// <param name="targetTenantId">The tenant targeted by the operation.</param>
    public Result Ensure(Guid? targetTenantId)
    {
        if (targetTenantId is not { } target || target == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidTenant);
        }

        if (_tenantContext.TenantId is not { } current || current != target)
        {
            return Result.Failure(IdentityApplicationErrors.CrossTenantForbidden);
        }

        return Result.Success();
    }
}
