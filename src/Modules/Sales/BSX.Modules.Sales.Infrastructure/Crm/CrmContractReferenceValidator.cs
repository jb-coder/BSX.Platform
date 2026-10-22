using BSX.Modules.Crm.Contracts.References;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Infrastructure.Crm;

/// <summary>
/// Validates Sales CRM references through the published <see cref="ICrmReferenceLookup"/> contract.
/// Sales never references the CRM domain, application or database directly.
/// </summary>
public sealed class CrmContractReferenceValidator : ICrmReferenceValidator
{
    private readonly ICrmReferenceLookup _lookup;

    /// <summary>Initializes a new instance of the <see cref="CrmContractReferenceValidator"/> class.</summary>
    /// <param name="lookup">The tenant-aware CRM reference contract.</param>
    public CrmContractReferenceValidator(ICrmReferenceLookup lookup) => _lookup = lookup;

    /// <inheritdoc />
    public Task<bool> CustomerExistsAsync(
        TenantId tenantId,
        CustomerId customerId,
        CancellationToken cancellationToken = default)
        => _lookup.CustomerExistsAsync(tenantId.Value, customerId.Value, cancellationToken);

    /// <inheritdoc />
    public Task<bool> OrganizationExistsAsync(
        TenantId tenantId,
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => _lookup.OrganizationExistsAsync(tenantId.Value, organizationId.Value, cancellationToken);
}
