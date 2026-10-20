using FluentValidation;

namespace BSX.Modules.Crm.Application.Contacts.SetPrimaryContact;

/// <summary>Validates <see cref="SetPrimaryContactCommand"/>.</summary>
public sealed class SetPrimaryContactCommandValidator : AbstractValidator<SetPrimaryContactCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SetPrimaryContactCommandValidator"/> class.</summary>
    public SetPrimaryContactCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.ContactId).NotEmpty();
    }
}
