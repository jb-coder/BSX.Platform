using FluentValidation;

namespace BSX.Modules.Crm.Application.Customers.AssignOrganization;

/// <summary>Validates <see cref="AssignOrganizationCommand"/>.</summary>
public sealed class AssignOrganizationCommandValidator : AbstractValidator<AssignOrganizationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="AssignOrganizationCommandValidator"/> class.</summary>
    public AssignOrganizationCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.OrganizationId).NotEmpty();
    }
}
