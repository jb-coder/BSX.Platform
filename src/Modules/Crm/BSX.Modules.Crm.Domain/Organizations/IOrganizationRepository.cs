using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.Organizations;

/// <summary>Persistence contract for the <see cref="Organization"/> aggregate (aggregate-specific).</summary>
public interface IOrganizationRepository
{
    /// <summary>Gets an organization by identifier.</summary>
    /// <param name="id">The organization identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new organization.</summary>
    /// <param name="organization">The organization.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);
}
