using FluentValidation;

namespace BSX.Modules.Inventory.Application.Warehouses.DeactivateWarehouse;

/// <summary>Validates <see cref="DeactivateWarehouseCommand"/>.</summary>
public sealed class DeactivateWarehouseCommandValidator : AbstractValidator<DeactivateWarehouseCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DeactivateWarehouseCommandValidator"/> class.</summary>
    public DeactivateWarehouseCommandValidator() => RuleFor(command => command.WarehouseId).NotEmpty();
}
