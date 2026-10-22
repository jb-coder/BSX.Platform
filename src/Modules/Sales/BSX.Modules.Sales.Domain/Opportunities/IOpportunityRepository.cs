using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.Opportunities;

/// <summary>Persistence contract for the <see cref="Opportunity"/> aggregate (aggregate-specific).</summary>
public interface IOpportunityRepository
{
    /// <summary>Gets an opportunity by identifier within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier (read isolation, Option A).</param>
    /// <param name="id">The opportunity identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<Opportunity?> GetByIdAsync(TenantId tenantId, OpportunityId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the tenant already has an opportunity created from the given lead.
    /// A qualified lead may produce only one opportunity.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="leadId">The source lead identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> ExistsByLeadIdAsync(TenantId tenantId, LeadId leadId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new opportunity.</summary>
    /// <param name="opportunity">The opportunity.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Opportunity opportunity, CancellationToken cancellationToken = default);
}
