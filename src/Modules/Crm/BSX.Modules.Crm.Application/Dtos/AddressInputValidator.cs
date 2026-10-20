using BSX.Modules.Crm.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Crm.Application.Dtos;

/// <summary>Validates the shape of an <see cref="AddressInput"/> (required fields and lengths).</summary>
public sealed class AddressInputValidator : AbstractValidator<AddressInput>
{
    /// <summary>Initializes a new instance of the <see cref="AddressInputValidator"/> class.</summary>
    public AddressInputValidator()
    {
        RuleFor(address => address.Line1).NotEmpty().MaximumLength(Address.MaxLineLength);
        RuleFor(address => address.Line2)
            .MaximumLength(Address.MaxLineLength)
            .When(address => !string.IsNullOrWhiteSpace(address.Line2));
        RuleFor(address => address.City).NotEmpty().MaximumLength(Address.MaxAreaLength);
        RuleFor(address => address.Region)
            .MaximumLength(Address.MaxAreaLength)
            .When(address => !string.IsNullOrWhiteSpace(address.Region));
        RuleFor(address => address.PostalCode)
            .MaximumLength(Address.MaxPostalCodeLength)
            .When(address => !string.IsNullOrWhiteSpace(address.PostalCode));
        RuleFor(address => address.Country).NotEmpty().MaximumLength(Address.MaxCountryLength);
    }
}
