using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.Abstractions;

/// <summary>Read-model access for organizations (CQRS read side). Implemented in Infrastructure.</summary>
public interface IOrganizationReadRepository
{
    /// <summary>Gets an organization projection within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<OrganizationDto?> GetByIdAsync(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Lists organization projections within a tenant, with paging and an optional search term.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="size">The page size.</param>
    /// <param name="search">An optional search term.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedResult<OrganizationDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default);
}
