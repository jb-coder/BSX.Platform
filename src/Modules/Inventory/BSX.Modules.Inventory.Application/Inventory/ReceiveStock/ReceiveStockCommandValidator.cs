using FluentValidation;

namespace BSX.Modules.Inventory.Application.Inventory.ReceiveStock;

/// <summary>Validates <see cref="ReceiveStockCommand"/> (shape only; the domain validates quantities).</summary>
public sealed class ReceiveStockCommandValidator : AbstractValidator<ReceiveStockCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ReceiveStockCommandValidator"/> class.</summary>
    public ReceiveStockCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.WarehouseId).NotEmpty();
    }
}
