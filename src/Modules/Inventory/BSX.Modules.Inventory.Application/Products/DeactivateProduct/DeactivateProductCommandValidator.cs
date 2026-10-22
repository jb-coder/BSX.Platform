using FluentValidation;

namespace BSX.Modules.Inventory.Application.Products.DeactivateProduct;

/// <summary>Validates <see cref="DeactivateProductCommand"/>.</summary>
public sealed class DeactivateProductCommandValidator : AbstractValidator<DeactivateProductCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DeactivateProductCommandValidator"/> class.</summary>
    public DeactivateProductCommandValidator() => RuleFor(command => command.ProductId).NotEmpty();
}
