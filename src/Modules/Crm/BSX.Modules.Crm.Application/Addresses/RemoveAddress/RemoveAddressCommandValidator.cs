using FluentValidation;

namespace BSX.Modules.Crm.Application.Addresses.RemoveAddress;

/// <summary>Validates <see cref="RemoveAddressCommand"/>.</summary>
public sealed class RemoveAddressCommandValidator : AbstractValidator<RemoveAddressCommand>
{
    /// <summary>Initializes a new instance of the <see cref="RemoveAddressCommandValidator"/> class.</summary>
    public RemoveAddressCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.AddressId).NotEmpty();
    }
}
