using FluentValidation;

namespace BSX.Modules.Identity.Application.Authentication.CompletePendingLogin;

/// <summary>Validates <see cref="CompletePendingLoginCommand"/>.</summary>
public sealed class CompletePendingLoginCommandValidator : AbstractValidator<CompletePendingLoginCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CompletePendingLoginCommandValidator"/> class.</summary>
    public CompletePendingLoginCommandValidator()
    {
        RuleFor(command => command.PendingLoginToken).NotEmpty();
        RuleFor(command => command.SelectionId).NotEmpty();
        RuleFor(command => command.TenantId).NotEmpty();
    }
}
