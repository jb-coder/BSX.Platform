using BSX.Modules.Inventory.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Inventory.Application.Warehouses.CreateWarehouse;

/// <summary>Validates <see cref="CreateWarehouseCommand"/> (shape only; the domain validates values).</summary>
public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateWarehouseCommandValidator"/> class.</summary>
    public CreateWarehouseCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(WarehouseName.MaxLength);
    }
}
