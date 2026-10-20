using BSX.Modules.Crm.Application.Dtos;
using FluentValidation;

namespace BSX.Modules.Crm.Application.Addresses.UpdateAddress;

/// <summary>Validates <see cref="UpdateAddressCommand"/>.</summary>
public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateAddressCommandValidator"/> class.</summary>
    public UpdateAddressCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.AddressId).NotEmpty();
        RuleFor(command => command.Address).NotNull();
        RuleFor(command => command.Address!).SetValidator(new AddressInputValidator());
        RuleFor(command => command.Type).IsInEnum();
    }
}
