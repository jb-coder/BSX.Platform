using FluentValidation;

namespace BSX.Modules.Inventory.Application.Warehouses.ActivateWarehouse;

/// <summary>Validates <see cref="ActivateWarehouseCommand"/>.</summary>
public sealed class ActivateWarehouseCommandValidator : AbstractValidator<ActivateWarehouseCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ActivateWarehouseCommandValidator"/> class.</summary>
    public ActivateWarehouseCommandValidator() => RuleFor(command => command.WarehouseId).NotEmpty();
}
