using BSX.Modules.Crm.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Crm.Application.Customers.UpdateCustomer;

/// <summary>Validates <see cref="UpdateCustomerCommand"/>.</summary>
public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateCustomerCommandValidator"/> class.</summary>
    public UpdateCustomerCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(CustomerName.MaxLength);
        RuleFor(command => command.TaxIdentifier)
            .MaximumLength(TaxIdentifier.MaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.TaxIdentifier));
    }
}
