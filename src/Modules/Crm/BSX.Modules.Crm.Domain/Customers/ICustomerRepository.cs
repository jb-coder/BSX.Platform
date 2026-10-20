using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.Customers;

/// <summary>Persistence contract for the <see cref="Customer"/> aggregate (aggregate-specific).</summary>
public interface ICustomerRepository
{
    /// <summary>Gets a customer by identifier within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier (read isolation, Option A).</param>
    /// <param name="id">The customer identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<Customer?> GetByIdAsync(TenantId tenantId, CustomerId id, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a customer code exists within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="code">The customer code.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> ExistsByCodeAsync(TenantId tenantId, CustomerCode code, CancellationToken cancellationToken = default);

    /// <summary>Adds a new customer.</summary>
    /// <param name="customer">The customer.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}
