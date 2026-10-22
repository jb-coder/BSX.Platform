using FluentValidation;

namespace BSX.Modules.Inventory.Application.Inventory.AdjustStock;

/// <summary>Validates <see cref="AdjustStockCommand"/> (shape only; the domain validates the delta).</summary>
public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    /// <summary>Initializes a new instance of the <see cref="AdjustStockCommandValidator"/> class.</summary>
    public AdjustStockCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.WarehouseId).NotEmpty();
    }
}
