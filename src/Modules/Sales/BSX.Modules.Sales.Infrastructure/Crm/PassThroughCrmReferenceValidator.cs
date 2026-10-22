using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Infrastructure.Crm;

/// <summary>
/// Temporary CRM reference validator used until a real implementation backed by CRM contracts is
/// available. It accepts every reference and therefore provides no cross-module validation.
/// Replace it in composition with a tenant-aware implementation before exposing Sales writes.
/// </summary>
public sealed class PassThroughCrmReferenceValidator : ICrmReferenceValidator
{
    /// <inheritdoc />
    public Task<bool> CustomerExistsAsync(
        TenantId tenantId,
        CustomerId customerId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    /// <inheritdoc />
    public Task<bool> OrganizationExistsAsync(
        TenantId tenantId,
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
