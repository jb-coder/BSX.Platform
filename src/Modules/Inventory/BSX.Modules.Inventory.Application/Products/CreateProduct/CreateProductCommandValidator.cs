using BSX.Modules.Inventory.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Inventory.Application.Products.CreateProduct;

/// <summary>Validates <see cref="CreateProductCommand"/> (shape only; the domain validates values).</summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateProductCommandValidator"/> class.</summary>
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Sku).NotEmpty().MaximumLength(Sku.MaxLength);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(ProductName.MaxLength);
    }
}
