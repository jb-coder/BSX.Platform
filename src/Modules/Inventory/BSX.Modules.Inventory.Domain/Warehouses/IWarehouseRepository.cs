using BSX.Modules.Inventory.Domain.ValueObjects;

namespace BSX.Modules.Inventory.Domain.Warehouses;

/// <summary>Persistence contract for the <see cref="Warehouse"/> aggregate (aggregate-specific).</summary>
public interface IWarehouseRepository
{
    /// <summary>Gets a warehouse by identifier within a tenant.</summary>
    /// <param name="tenantId">The tenant identifier (read isolation).</param>
    /// <param name="id">The warehouse identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<Warehouse?> GetByIdAsync(TenantId tenantId, WarehouseId id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new warehouse.</summary>
    /// <param name="warehouse">The warehouse.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
}
