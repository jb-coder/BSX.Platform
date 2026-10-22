using BSX.Modules.Sales.Domain.Opportunities;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.Modules.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.Repositories;

internal sealed class OpportunityRepository : IOpportunityRepository
{
    private readonly SalesDbContext _context;

    public OpportunityRepository(SalesDbContext context) => _context = context;

    public Task<Opportunity?> GetByIdAsync(
        TenantId tenantId,
        OpportunityId id,
        CancellationToken cancellationToken = default)
        => _context.Opportunities.FirstOrDefaultAsync(
            opportunity => opportunity.Id == id && opportunity.TenantId == tenantId,
            cancellationToken);

    public Task<bool> ExistsByLeadIdAsync(
        TenantId tenantId,
        LeadId leadId,
        CancellationToken cancellationToken = default)
        => _context.Opportunities.AnyAsync(
            opportunity => opportunity.TenantId == tenantId && opportunity.LeadId == leadId,
            cancellationToken);

    public async Task AddAsync(Opportunity opportunity, CancellationToken cancellationToken = default)
    {
        await _context.Opportunities.AddAsync(opportunity, cancellationToken).ConfigureAwait(false);
    }
}
