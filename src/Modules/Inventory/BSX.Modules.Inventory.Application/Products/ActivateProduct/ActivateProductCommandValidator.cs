using FluentValidation;

namespace BSX.Modules.Inventory.Application.Products.ActivateProduct;

/// <summary>Validates <see cref="ActivateProductCommand"/>.</summary>
public sealed class ActivateProductCommandValidator : AbstractValidator<ActivateProductCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ActivateProductCommandValidator"/> class.</summary>
    public ActivateProductCommandValidator() => RuleFor(command => command.ProductId).NotEmpty();
}
