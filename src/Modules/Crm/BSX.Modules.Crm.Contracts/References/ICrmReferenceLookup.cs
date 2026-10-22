namespace BSX.Modules.Crm.Contracts.References;

/// <summary>
/// Read-only CRM contract used by other modules to validate that a customer or organization
/// reference exists in a tenant. Implementations are tenant-aware and fail closed: a reference is
/// reported as existing only when it is visible in the supplied tenant.
/// </summary>
public interface ICrmReferenceLookup
{
    /// <summary>Determines whether a customer exists in the tenant.</summary>
    /// <param name="tenantId">The tenant that must own the customer.</param>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> CustomerExistsAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Determines whether an organization exists in the tenant.</summary>
    /// <param name="tenantId">The tenant that must own the organization.</param>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> OrganizationExistsAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
