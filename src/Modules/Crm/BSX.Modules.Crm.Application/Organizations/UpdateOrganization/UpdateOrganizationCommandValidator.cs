using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Crm.Application.Organizations.UpdateOrganization;

/// <summary>Validates <see cref="UpdateOrganizationCommand"/>.</summary>
public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateOrganizationCommandValidator"/> class.</summary>
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(CustomerName.MaxLength);
        RuleFor(command => command.TaxIdentifier)
            .MaximumLength(TaxIdentifier.MaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.TaxIdentifier));
        RuleFor(command => command.Address!).SetValidator(new AddressInputValidator());
    }
}
