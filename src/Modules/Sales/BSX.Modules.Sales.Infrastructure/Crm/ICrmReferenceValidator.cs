using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Infrastructure.Crm;

/// <summary>
/// Infrastructure seam for validating CRM integration references. Implementations must be
/// tenant-aware and must use CRM contracts/interfaces only — never the CRM domain or database.
/// </summary>
public interface ICrmReferenceValidator
{
    /// <summary>Determines whether a customer reference exists in the tenant.</summary>
    /// <param name="tenantId">The tenant that owns the reference.</param>
    /// <param name="customerId">The CRM customer reference.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> CustomerExistsAsync(
        TenantId tenantId,
        CustomerId customerId,
        CancellationToken cancellationToken = default);

    /// <summary>Determines whether an organization reference exists in the tenant.</summary>
    /// <param name="tenantId">The tenant that owns the reference.</param>
    /// <param name="organizationId">The CRM organization reference.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> OrganizationExistsAsync(
        TenantId tenantId,
        OrganizationId organizationId,
        CancellationToken cancellationToken = default);
}
