using FluentValidation;

namespace BSX.Modules.Crm.Application.Customers.ActivateCustomer;

/// <summary>Validates <see cref="ActivateCustomerCommand"/>.</summary>
public sealed class ActivateCustomerCommandValidator : AbstractValidator<ActivateCustomerCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ActivateCustomerCommandValidator"/> class.</summary>
    public ActivateCustomerCommandValidator() => RuleFor(command => command.CustomerId).NotEmpty();
}
