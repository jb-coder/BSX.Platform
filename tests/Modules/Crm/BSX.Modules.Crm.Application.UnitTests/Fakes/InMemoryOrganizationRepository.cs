using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Application.UnitTests.Fakes;

internal sealed class InMemoryOrganizationRepository : IOrganizationRepository
{
    private readonly List<Organization> _organizations = [];

    public IReadOnlyCollection<Organization> Organizations => _organizations;

    public TenantId? LastRequestedTenant { get; private set; }

    public void Seed(params Organization[] organizations) => _organizations.AddRange(organizations);

    public Task<Organization?> GetByIdAsync(TenantId tenantId, OrganizationId id, CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_organizations.Find(organization => organization.Id == id && organization.TenantId == tenantId));
    }

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _organizations.Add(organization);
        return Task.CompletedTask;
    }
}
