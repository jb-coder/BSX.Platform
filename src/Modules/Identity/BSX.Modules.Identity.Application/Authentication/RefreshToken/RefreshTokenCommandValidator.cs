using FluentValidation;

namespace BSX.Modules.Identity.Application.Authentication.RefreshToken;

/// <summary>Validates <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>Initializes a new instance of the <see cref="RefreshTokenCommandValidator"/> class.</summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
    }
}
