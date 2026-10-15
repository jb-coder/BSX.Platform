using FluentValidation;

namespace BSX.Modules.Identity.Application.Authentication.Login;

/// <summary>Validates <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Initializes a new instance of the <see cref="LoginCommandValidator"/> class.</summary>
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(command => command.Password).NotEmpty();
    }
}
