using FluentValidation;

namespace BSX.Modules.Inventory.Application.Inventory.ReserveStock;

/// <summary>Validates <see cref="ReserveStockCommand"/> (shape only; the domain validates quantities).</summary>
public sealed class ReserveStockCommandValidator : AbstractValidator<ReserveStockCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ReserveStockCommandValidator"/> class.</summary>
    public ReserveStockCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.WarehouseId).NotEmpty();
    }
}
