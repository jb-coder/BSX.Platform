using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Application.UnitTests.Fakes;

internal sealed class InMemoryOpportunityRepository : IOpportunityRepository
{
    private readonly List<Opportunity> _opportunities = [];

    public IReadOnlyCollection<Opportunity> Opportunities => _opportunities;

    public TenantId? LastRequestedTenant { get; private set; }

    public LeadId? LastRequestedLeadId { get; private set; }

    public void Seed(params Opportunity[] opportunities) => _opportunities.AddRange(opportunities);

    public Task<Opportunity?> GetByIdAsync(
        TenantId tenantId,
        OpportunityId id,
        CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        return Task.FromResult(_opportunities.Find(
            opportunity => opportunity.Id == id && opportunity.TenantId == tenantId));
    }

    public Task<bool> ExistsByLeadIdAsync(
        TenantId tenantId,
        LeadId leadId,
        CancellationToken cancellationToken = default)
    {
        LastRequestedTenant = tenantId;
        LastRequestedLeadId = leadId;
        return Task.FromResult(_opportunities.Exists(
            opportunity => opportunity.TenantId == tenantId && opportunity.LeadId == leadId));
    }

    public Task AddAsync(Opportunity opportunity, CancellationToken cancellationToken = default)
    {
        _opportunities.Add(opportunity);
        return Task.CompletedTask;
    }
}
