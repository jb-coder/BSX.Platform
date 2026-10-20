using FluentValidation;

namespace BSX.Modules.Crm.Application.Addresses.SetDefaultAddress;

/// <summary>Validates <see cref="SetDefaultAddressCommand"/>.</summary>
public sealed class SetDefaultAddressCommandValidator : AbstractValidator<SetDefaultAddressCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SetDefaultAddressCommandValidator"/> class.</summary>
    public SetDefaultAddressCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.AddressId).NotEmpty();
    }
}
