using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.Modules.Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Crm.Infrastructure.Repositories;

internal sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly CrmDbContext _context;

    public OrganizationRepository(CrmDbContext context) => _context = context;

    public Task<Organization?> GetByIdAsync(
        TenantId tenantId,
        OrganizationId id,
        CancellationToken cancellationToken = default)
        => _context.Organizations
            .Include(organization => organization.Address)
            .FirstOrDefaultAsync(
                organization => organization.Id == id && organization.TenantId == tenantId,
                cancellationToken);

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        await _context.Organizations.AddAsync(organization, cancellationToken).ConfigureAwait(false);
    }
}
