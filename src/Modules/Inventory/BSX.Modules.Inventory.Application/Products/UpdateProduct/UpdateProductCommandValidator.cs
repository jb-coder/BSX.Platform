using BSX.Modules.Inventory.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Inventory.Application.Products.UpdateProduct;

/// <summary>Validates <see cref="UpdateProductCommand"/> (shape only; the domain validates values).</summary>
public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateProductCommandValidator"/> class.</summary>
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(ProductName.MaxLength);
    }
}
