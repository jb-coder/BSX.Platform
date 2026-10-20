using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.Abstractions;

/// <summary>Read-model access for customers (CQRS read side). Implemented in Infrastructure.</summary>
public interface ICustomerReadRepository
{
    /// <summary>Gets a customer projection within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<CustomerDto?> GetByIdAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Lists customer summaries within a tenant, with paging and an optional search term.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="page">The one-based page number.</param>
    /// <param name="size">The page size.</param>
    /// <param name="search">An optional search term.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedResult<CustomerSummaryDto>> ListAsync(
        Guid tenantId,
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default);
}
