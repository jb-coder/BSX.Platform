using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.ServiceAccounts;

/// <summary>Persistence contract for the <see cref="ApiKey"/> aggregate.</summary>
public interface IApiKeyRepository
{
    /// <summary>Gets an API key by identifier.</summary>
    /// <param name="id">The API key identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<ApiKey?> GetByIdAsync(ApiKeyId id, CancellationToken cancellationToken = default);

    /// <summary>Lists API keys for a principal.</summary>
    /// <param name="principalId">The principal identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<IReadOnlyCollection<ApiKey>> ListByPrincipalAsync(PrincipalId principalId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new API key.</summary>
    /// <param name="apiKey">The API key.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default);
}
