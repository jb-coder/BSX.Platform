using FluentValidation;

namespace BSX.Modules.Identity.Application.Roles.UpdateRole;

/// <summary>Validates <see cref="UpdateRoleCommand"/>.</summary>
public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateRoleCommandValidator"/> class.</summary>
    public UpdateRoleCommandValidator()
    {
        RuleFor(command => command.RoleId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Description).MaximumLength(500);
    }
}
