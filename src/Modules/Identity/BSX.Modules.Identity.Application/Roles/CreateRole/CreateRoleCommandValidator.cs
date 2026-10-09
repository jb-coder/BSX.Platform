using FluentValidation;

namespace BSX.Modules.Identity.Application.Roles.CreateRole;

/// <summary>Validates <see cref="CreateRoleCommand"/>.</summary>
public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateRoleCommandValidator"/> class.</summary>
    public CreateRoleCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Description).MaximumLength(500);
        RuleFor(command => command.Permissions).NotNull();
        RuleForEach(command => command.Permissions).NotEmpty();
    }
}
