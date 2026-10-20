using BSX.Modules.Crm.Domain.ValueObjects;
using FluentValidation;

namespace BSX.Modules.Crm.Application.Contacts.UpdateContact;

/// <summary>Validates <see cref="UpdateContactCommand"/>.</summary>
public sealed class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateContactCommandValidator"/> class.</summary>
    public UpdateContactCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.ContactId).NotEmpty();
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
