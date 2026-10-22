using BSX.Modules.Crm.Contracts.References;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;

internal sealed class FakeCrmReferenceLookup : ICrmReferenceLookup
{
    public bool CustomerExists { get; set; }

    public bool OrganizationExists { get; set; }

    public List<(Guid TenantId, Guid ReferenceId)> CustomerCalls { get; } = [];

    public List<(Guid TenantId, Guid ReferenceId)> OrganizationCalls { get; } = [];

    public Task<bool> CustomerExistsAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        CustomerCalls.Add((tenantId, customerId));
        return Task.FromResult(CustomerExists);
    }

    public Task<bool> OrganizationExistsAsync(
        Guid tenantId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        OrganizationCalls.Add((tenantId, organizationId));
        return Task.FromResult(OrganizationExists);
    }
}
