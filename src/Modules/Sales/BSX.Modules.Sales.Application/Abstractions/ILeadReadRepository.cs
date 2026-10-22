using BSX.Modules.Sales.Application.Dtos;

namespace BSX.Modules.Sales.Application.Abstractions;

/// <summary>Read-model access for leads (CQRS read side). Implemented in Infrastructure.</summary>
public interface ILeadReadRepository
{
    /// <summary>Gets a lead projection within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="leadId">The lead identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<LeadDto?> GetByIdAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default);

    /// <summary>Lists lead summaries within a tenant, with paging and an optional search term.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="size">The page size.</param>
    /// <param name="search">An optional title search term.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedResult<LeadSummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default);
}
