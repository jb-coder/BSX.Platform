using FluentValidation;

namespace BSX.Modules.Crm.Application.Contacts.RemoveContact;

/// <summary>Validates <see cref="RemoveContactCommand"/>.</summary>
public sealed class RemoveContactCommandValidator : AbstractValidator<RemoveContactCommand>
{
    /// <summary>Initializes a new instance of the <see cref="RemoveContactCommandValidator"/> class.</summary>
    public RemoveContactCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.ContactId).NotEmpty();
    }
}
