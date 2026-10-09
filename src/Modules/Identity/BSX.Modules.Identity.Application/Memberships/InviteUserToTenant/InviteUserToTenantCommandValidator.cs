using FluentValidation;

namespace BSX.Modules.Identity.Application.Memberships.InviteUserToTenant;

/// <summary>Validates <see cref="InviteUserToTenantCommand"/>.</summary>
public sealed class InviteUserToTenantCommandValidator : AbstractValidator<InviteUserToTenantCommand>
{
    /// <summary>Initializes a new instance of the <see cref="InviteUserToTenantCommandValidator"/> class.</summary>
    public InviteUserToTenantCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.UserId).NotEmpty();
    }
}
