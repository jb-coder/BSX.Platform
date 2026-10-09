using FluentValidation;

namespace BSX.Modules.Identity.Application.Users.RegisterUser;

/// <summary>Validates <see cref="RegisterUserCommand"/>.</summary>
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    /// <summary>Initializes a new instance of the <see cref="RegisterUserCommandValidator"/> class.</summary>
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Password)
            .MinimumLength(8)
            .When(command => !string.IsNullOrWhiteSpace(command.Password));
    }
}
