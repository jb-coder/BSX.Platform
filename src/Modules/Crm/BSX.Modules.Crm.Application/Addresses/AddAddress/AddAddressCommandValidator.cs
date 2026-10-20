using BSX.Modules.Crm.Application.Dtos;
using FluentValidation;

namespace BSX.Modules.Crm.Application.Addresses.AddAddress;

/// <summary>Validates <see cref="AddAddressCommand"/> (shape only; the domain validates the address).</summary>
public sealed class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    /// <summary>Initializes a new instance of the <see cref="AddAddressCommandValidator"/> class.</summary>
    public AddAddressCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Address).NotNull();
        RuleFor(command => command.Address!).SetValidator(new AddressInputValidator());
        RuleFor(command => command.Type).IsInEnum();
    }
}
