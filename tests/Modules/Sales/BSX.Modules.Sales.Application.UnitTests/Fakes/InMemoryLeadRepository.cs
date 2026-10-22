using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Application.UnitTests.Fakes;

internal sealed class InMemoryLeadRepository : ILeadRepository
{
    private readonly List<Lead> _leads = [];

    public IReadOnlyCollection<Lead> Leads => _leads;

    public TenantId? LastRequestedTenant { get; private set; }

    public void Seed(params Lead[] leads) => _leads.AddRange(leads);

    public Task<Lead?> GetByIdAsync(TenantId tenantId, LeadId id, CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_leads.Find(lead => lead.Id == id && lead.TenantId == tenantId));
    }

    public Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        _leads.Add(lead);
        return Task.CompletedTask;
    }
}
