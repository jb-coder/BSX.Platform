using FluentValidation;

namespace BSX.Modules.Crm.Application.Customers.DeactivateCustomer;

/// <summary>Validates <see cref="DeactivateCustomerCommand"/>.</summary>
public sealed class DeactivateCustomerCommandValidator : AbstractValidator<DeactivateCustomerCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DeactivateCustomerCommandValidator"/> class.</summary>
    public DeactivateCustomerCommandValidator() => RuleFor(command => command.CustomerId).NotEmpty();
}
