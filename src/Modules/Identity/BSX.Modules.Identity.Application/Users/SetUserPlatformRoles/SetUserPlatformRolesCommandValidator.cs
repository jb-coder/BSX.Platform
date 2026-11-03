using FluentValidation;

namespace BSX.Modules.Identity.Application.Users.SetUserPlatformRoles;

/// <summary>Validates <see cref="SetUserPlatformRolesCommand"/>.</summary>
public sealed class SetUserPlatformRolesCommandValidator : AbstractValidator<SetUserPlatformRolesCommand>
{
    /// <summary>Initializes a new instance of the <see cref="SetUserPlatformRolesCommandValidator"/> class.</summary>
    public SetUserPlatformRolesCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RoleIds).NotNull();
        RuleForEach(command => command.RoleIds).NotEmpty();
        RuleFor(command => command.RoleIds)
            .Must(roleIds => roleIds.Distinct().Count() == roleIds.Count)
            .When(command => command.RoleIds is not null)
            .WithMessage("Duplicate platform role identifiers are not allowed.");
    }
}
