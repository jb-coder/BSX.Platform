using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.Modules.Sales.Infrastructure.Crm;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;

internal sealed class TestCrmReferenceValidator : ICrmReferenceValidator
{
    public bool AcceptAll { get; set; } = true;

    public HashSet<Guid> Customers { get; } = [];

    public HashSet<Guid> Organizations { get; } = [];

    public List<(Guid TenantId, Guid ReferenceId)> CustomerCalls { get; } = [];

    public List<(Guid TenantId, Guid ReferenceId)> OrganizationCalls { get; } = [];

    public Task<bool> CustomerExistsAsync(
        TenantId tenantId,
        CustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        CustomerCalls.Add((tenantId.Value, customerId.Value));
        return Task.FromResult(AcceptAll || Customers.Contains(customerId.Value));
    }

    public Task<bool> OrganizationExistsAsync(
        TenantId tenantId,
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        OrganizationCalls.Add((tenantId.Value, organizationId.Value));
        return Task.FromResult(AcceptAll || Organizations.Contains(organizationId.Value));
    }
}
