using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Repositories;

internal sealed class ServiceAccountRepository : IServiceAccountRepository
{
    private readonly IdentityDbContext _context;

    public ServiceAccountRepository(IdentityDbContext context) => _context = context;

    public Task<ServiceAccount?> GetByIdAsync(ServiceAccountId id, CancellationToken cancellationToken = default)
        => _context.ServiceAccounts.FirstOrDefaultAsync(account => account.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ServiceAccount>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        List<ServiceAccount> accounts = await _context.ServiceAccounts
            .Where(account => account.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return accounts;
    }

    public async Task AddAsync(ServiceAccount serviceAccount, CancellationToken cancellationToken = default)
        => await _context.ServiceAccounts.AddAsync(serviceAccount, cancellationToken).ConfigureAwait(false);
}
