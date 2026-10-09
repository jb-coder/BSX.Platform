using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Repositories;

internal sealed class ApiKeyRepository : IApiKeyRepository
{
    private readonly IdentityDbContext _context;

    public ApiKeyRepository(IdentityDbContext context) => _context = context;

    public Task<ApiKey?> GetByIdAsync(ApiKeyId id, CancellationToken cancellationToken = default)
        => _context.ApiKeys.FirstOrDefaultAsync(apiKey => apiKey.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<ApiKey>> ListByPrincipalAsync(PrincipalId principalId, CancellationToken cancellationToken = default)
    {
        List<ApiKey> apiKeys = await _context.ApiKeys
            .Where(apiKey => apiKey.PrincipalId == principalId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return apiKeys;
    }

    public async Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
        => await _context.ApiKeys.AddAsync(apiKey, cancellationToken).ConfigureAwait(false);
}
