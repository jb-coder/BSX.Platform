using BSX.Modules.Sales.Domain.Leads;
using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.Modules.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Sales.Infrastructure.Repositories;

internal sealed class LeadRepository : ILeadRepository
{
    private readonly SalesDbContext _context;

    public LeadRepository(SalesDbContext context) => _context = context;

    public Task<Lead?> GetByIdAsync(
        TenantId tenantId,
        LeadId id,
        CancellationToken cancellationToken = default)
        => _context.Leads.FirstOrDefaultAsync(
            lead => lead.Id == id && lead.TenantId == tenantId,
            cancellationToken);

    public async Task AddAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        await _context.Leads.AddAsync(lead, cancellationToken).ConfigureAwait(false);
    }
}
