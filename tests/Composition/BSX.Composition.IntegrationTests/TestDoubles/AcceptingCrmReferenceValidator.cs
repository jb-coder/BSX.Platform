using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.Modules.Sales.Infrastructure.Crm;

namespace BSX.Composition.IntegrationTests.TestDoubles;

internal sealed class AcceptingCrmReferenceValidator : ICrmReferenceValidator
{
    public Task<bool> CustomerExistsAsync(
        TenantId tenantId,
        CustomerId customerId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> OrganizationExistsAsync(
        TenantId tenantId,
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
