using BSX.Modules.Crm.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Crm.Application.Contacts.AddContact;

/// <summary>Validates <see cref="AddContactCommand"/> (shape only; reachability is a domain rule).</summary>
public sealed class AddContactCommandValidator : AbstractValidator<AddContactCommand>
{
    /// <summary>Initializes a new instance of the <see cref="AddContactCommandValidator"/> class.</summary>
    public AddContactCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(CustomerName.MaxLength);
        RuleFor(command => command.Email)
            .MaximumLength(Email.MaxLength)
            .EmailAddress()
            .When(command => !string.IsNullOrWhiteSpace(command.Email));
        RuleFor(command => command.PhoneNumber)
            .MaximumLength(PhoneNumber.MaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.PhoneNumber));
    }
}
