using BSX.Modules.Sales.Domain.ValueObjects;

namespace BSX.Modules.Sales.Domain.Leads;

/// <summary>Persistence contract for the <see cref="Lead"/> aggregate (aggregate-specific).</summary>
public interface ILeadRepository
{
    /// <summary>Gets a lead by identifier within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier (read isolation, Option A).</param>
    /// <param name="id">The lead identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<Lead?> GetByIdAsync(TenantId tenantId, LeadId id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new lead.</summary>
    /// <param name="lead">The lead.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Lead lead, CancellationToken cancellationToken = default);
}
