using BSX.Modules.Crm.Application.Dtos;
using BSX.Modules.Crm.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Crm.Application.Organizations.CreateOrganization;

/// <summary>Validates <see cref="CreateOrganizationCommand"/>.</summary>
public sealed class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateOrganizationCommandValidator"/> class.</summary>
    public CreateOrganizationCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(CustomerName.MaxLength);
        RuleFor(command => command.TaxIdentifier)
            .MaximumLength(TaxIdentifier.MaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.TaxIdentifier));
        RuleFor(command => command.Address!).SetValidator(new AddressInputValidator());
    }
}
