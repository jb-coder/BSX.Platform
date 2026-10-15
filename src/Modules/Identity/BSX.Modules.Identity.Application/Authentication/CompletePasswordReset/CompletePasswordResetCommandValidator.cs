using FluentValidation;

namespace BSX.Modules.Identity.Application.Authentication.CompletePasswordReset;

/// <summary>Validates <see cref="CompletePasswordResetCommand"/>.</summary>
public sealed class CompletePasswordResetCommandValidator : AbstractValidator<CompletePasswordResetCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CompletePasswordResetCommandValidator"/> class.</summary>
    public CompletePasswordResetCommandValidator()
    {
        RuleFor(command => command.Token).NotEmpty();
        RuleFor(command => command.NewPassword).NotEmpty().MinimumLength(8);
    }
}
