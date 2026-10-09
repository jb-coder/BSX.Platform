using FluentValidation;

namespace BSX.Modules.Identity.Application.ServiceAccounts.CreateServiceAccount;

/// <summary>Validates <see cref="CreateServiceAccountCommand"/>.</summary>
public sealed class CreateServiceAccountCommandValidator : AbstractValidator<CreateServiceAccountCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateServiceAccountCommandValidator"/> class.</summary>
    public CreateServiceAccountCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
    }
}
