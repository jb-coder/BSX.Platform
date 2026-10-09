using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.ServiceAccounts;

/// <summary>Persistence contract for the <see cref="ServiceAccount"/> aggregate.</summary>
public interface IServiceAccountRepository
{
    /// <summary>Gets a service account by identifier.</summary>
    /// <param name="id">The service account identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<ServiceAccount?> GetByIdAsync(ServiceAccountId id, CancellationToken cancellationToken = default);

    /// <summary>Lists service accounts for a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<IReadOnlyCollection<ServiceAccount>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new service account.</summary>
    /// <param name="serviceAccount">The service account.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(ServiceAccount serviceAccount, CancellationToken cancellationToken = default);
}
