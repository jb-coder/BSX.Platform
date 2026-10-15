using FluentValidation;

namespace BSX.Modules.Identity.Application.Authentication.RequestPasswordReset;

/// <summary>Validates <see cref="RequestPasswordResetCommand"/>.</summary>
public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    /// <summary>Initializes a new instance of the <see cref="RequestPasswordResetCommandValidator"/> class.</summary>
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(320).EmailAddress();
    }
}
