using BSX.Modules.Inventory.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Inventory.Application.Warehouses.UpdateWarehouse;

/// <summary>Validates <see cref="UpdateWarehouseCommand"/> (shape only; the domain validates values).</summary>
public sealed class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateWarehouseCommandValidator"/> class.</summary>
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(command => command.WarehouseId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(WarehouseName.MaxLength);
    }
}
