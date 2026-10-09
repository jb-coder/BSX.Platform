using BSX.Modules.Identity.Domain.ServiceAccounts;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>In-memory <see cref="IApiKeyRepository"/> for handler tests.</summary>
internal sealed class InMemoryApiKeyRepository : IApiKeyRepository
{
    private readonly List<ApiKey> _keys = [];

    public IReadOnlyCollection<ApiKey> Keys => _keys;

    public void Seed(params ApiKey[] keys) => _keys.AddRange(keys);

    public Task<ApiKey?> GetByIdAsync(ApiKeyId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_keys.Find(key => key.Id == id));

    public Task<IReadOnlyCollection<ApiKey>> ListByPrincipalAsync(PrincipalId principalId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<ApiKey>>(_keys.Where(key => key.PrincipalId == principalId).ToList());

    public Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        _keys.Add(apiKey);
        return Task.CompletedTask;
    }
}
