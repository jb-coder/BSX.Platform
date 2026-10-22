using BSX.Modules.Inventory.Domain.DomainEvents;
using BSX.Modules.Inventory.Domain.Enums;
using BSX.Modules.Inventory.Domain.Errors;
using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Inventory.Domain.Warehouses;

/// <summary>
/// A physical or logical stock location. Warehouses are tenant-owned and expose an operational
/// active/inactive status (ADR-013).
/// </summary>
public sealed class Warehouse : AggregateRoot<WarehouseId>
{
    private Warehouse(WarehouseId id, TenantId tenantId, WarehouseName name)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Status = WarehouseStatus.Active;
    }

    private Warehouse()
    {
    }

    /// <summary>Gets the tenant identifier.</summary>
    public TenantId TenantId { get; private set; } = null!;

    /// <summary>Gets the warehouse name.</summary>
    public WarehouseName Name { get; private set; } = null!;

    /// <summary>Gets the operational status.</summary>
    public WarehouseStatus Status { get; private set; }

    /// <summary>Gets a value indicating whether the warehouse is operational.</summary>
    public bool IsOperational => Status == WarehouseStatus.Active;

    /// <summary>Creates an operational warehouse.</summary>
    /// <param name="id">The warehouse identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="name">The warehouse name.</param>
    public static Result<Warehouse> Create(WarehouseId id, TenantId tenantId, WarehouseName name)
    {
        var warehouse = new Warehouse(id, tenantId, name);
        warehouse.RaiseDomainEvent(new WarehouseCreated(id, name));
        return Result.Success(warehouse);
    }

    /// <summary>Renames the warehouse. Allowed in any status.</summary>
    /// <param name="name">The new name.</param>
    public Result Rename(WarehouseName name)
    {
        Name = name;
        return Result.Success();
    }

    /// <summary>Activates a deactivated warehouse.</summary>
    public Result Activate()
    {
        if (Status == WarehouseStatus.Active)
        {
            return Result.Failure(InventoryErrors.WarehouseAlreadyActive);
        }

        Status = WarehouseStatus.Active;
        RaiseDomainEvent(new WarehouseActivated(Id));
        return Result.Success();
    }

    /// <summary>Deactivates an active warehouse.</summary>
    public Result Deactivate()
    {
        if (Status == WarehouseStatus.Inactive)
        {
            return Result.Failure(InventoryErrors.WarehouseAlreadyInactive);
        }

        Status = WarehouseStatus.Inactive;
        RaiseDomainEvent(new WarehouseDeactivated(Id));
        return Result.Success();
    }
}
