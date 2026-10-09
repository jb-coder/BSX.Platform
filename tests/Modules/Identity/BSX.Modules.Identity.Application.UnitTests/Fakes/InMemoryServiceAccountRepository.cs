using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>In-memory <see cref="IServiceAccountRepository"/> for handler tests.</summary>
internal sealed class InMemoryServiceAccountRepository : IServiceAccountRepository
{
    private readonly List<ServiceAccount> _accounts = [];

    public IReadOnlyCollection<ServiceAccount> Accounts => _accounts;

    public void Seed(params ServiceAccount[] accounts) => _accounts.AddRange(accounts);

    public Task<ServiceAccount?> GetByIdAsync(ServiceAccountId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_accounts.Find(account => account.Id == id));

    public Task<IReadOnlyCollection<ServiceAccount>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<ServiceAccount>>(_accounts.Where(account => account.TenantId == tenantId).ToList());

    public Task AddAsync(ServiceAccount serviceAccount, CancellationToken cancellationToken = default)
    {
        _accounts.Add(serviceAccount);
        return Task.CompletedTask;
    }
}
