using BSX.Modules.Crm.Contracts.References;

namespace BSX.Modules.Sales.Infrastructure.Crm;

/// <summary>
/// Default CRM reference lookup used when the CRM module is not composed. It fails closed: every
/// reference is reported as missing, so Sales cannot persist an unverifiable customer or
/// organization reference.
/// </summary>
public sealed class FailClosedCrmReferenceLookup : ICrmReferenceLookup
{
    /// <inheritdoc />
    public Task<bool> CustomerExistsAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    /// <inheritdoc />
    public Task<bool> OrganizationExistsAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
