using FluentValidation;

namespace BSX.Modules.Crm.Application.Customers.ClearOrganization;

/// <summary>Validates <see cref="ClearOrganizationCommand"/>.</summary>
public sealed class ClearOrganizationCommandValidator : AbstractValidator<ClearOrganizationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ClearOrganizationCommandValidator"/> class.</summary>
    public ClearOrganizationCommandValidator() => RuleFor(command => command.CustomerId).NotEmpty();
}
