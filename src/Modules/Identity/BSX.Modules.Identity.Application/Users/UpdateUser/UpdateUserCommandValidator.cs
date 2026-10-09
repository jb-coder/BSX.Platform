using FluentValidation;

namespace BSX.Modules.Identity.Application.Users.UpdateUser;

/// <summary>Validates <see cref="UpdateUserCommand"/>.</summary>
public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateUserCommandValidator"/> class.</summary>
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Email).NotEmpty().MaximumLength(320).EmailAddress();
    }
}
