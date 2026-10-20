using BSX.Modules.Crm.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Crm.Application.Customers.CreateCustomer;

/// <summary>Validates <see cref="CreateCustomerCommand"/> (shape only; the domain validates values).</summary>
public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateCustomerCommandValidator"/> class.</summary>
    public CreateCustomerCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(CustomerCode.MaxLength);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(CustomerName.MaxLength);
        RuleFor(command => command.TaxIdentifier)
            .MaximumLength(TaxIdentifier.MaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.TaxIdentifier));
    }
}
