using FluentValidation;

namespace BSX.Modules.Identity.Application.Roles.SetRolePermissions;

/// <summary>Validates <see cref="SetRolePermissionsCommand"/>.</summary>
public sealed class SetRolePermissionsCommandValidator : AbstractValidator<SetRolePermissionsCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SetRolePermissionsCommandValidator"/> class.</summary>
    public SetRolePermissionsCommandValidator()
    {
        RuleFor(command => command.RoleId).NotEmpty();
        RuleFor(command => command.Permissions).NotNull();
        RuleForEach(command => command.Permissions).NotEmpty();
    }
}
