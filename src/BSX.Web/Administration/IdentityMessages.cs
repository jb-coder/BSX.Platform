using BSX.SharedKernel.Results;

namespace BSX.Web.Administration;

/// <summary>Maps Identity application and domain errors to readable administration messages.</summary>
internal static class IdentityMessages
{
    /// <summary>Returns a user-facing message for an Identity error.</summary>
    public static string For(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Code switch
        {
            "Identity.User.NotFound" => "The user no longer exists.",
            "Identity.User.EmailAlreadyExists" => "A user with that email address already exists.",
            "Identity.User.InvalidId" => "The user identifier is invalid.",
            "Identity.User.AlreadyActive" => "This user is already active.",
            "Identity.User.AlreadyDeactivated" => "This user is already deactivated.",
            "Identity.User.CredentialRequired" => "The user needs a password credential before activation.",
            "Identity.User.Email.Invalid" => "Enter a valid email address.",
            "Identity.User.Name.Invalid" => "Enter a valid display name.",
            "Identity.Role.NotFound" => "The role no longer exists.",
            "Identity.Role.InvalidId" => "The role identifier is invalid.",
            "Identity.Role.NameAlreadyExists" => "A role with that name already exists in this workspace.",
            "Identity.Role.Name.Invalid" => "Enter a valid role name of 100 characters or fewer.",
            "Identity.Role.SystemImmutable" => "System roles cannot be renamed or have their permissions changed.",
            "Identity.Membership.NotFound" => "The membership no longer exists.",
            "Identity.Membership.InvalidId" => "The membership identifier is invalid.",
            "Identity.Membership.AlreadyExists" => "That user is already a member of this workspace.",
            "Identity.Membership.InvalidTransition" => "That membership action is not valid for its current status.",
            "Identity.Membership.RoleAlreadyAssigned" => "That role is already assigned to the membership.",
            "Identity.Membership.RoleNotAssigned" => "That role is not assigned to the membership.",
            "Identity.Permission.Invalid" => "One of the selected permissions is invalid.",
            "Identity.Role.TenantMismatch" => "That role belongs to another workspace.",
            "Identity.Tenant.InvalidId" => "The workspace identifier is invalid.",
            "Authorization.TenantForbidden" => "You cannot manage records outside the active workspace.",
            "Identity.Concurrency.Conflict" => "This record changed while you were editing it. Reload and try again.",
            "Identity.Session.NotFound" => "The session no longer exists.",
            "Identity.Session.Forbidden" => "You can only revoke your own sessions.",
            _ => string.IsNullOrWhiteSpace(error.Message)
                ? "The request could not be completed. Please try again."
                : error.Message,
        };
    }
}
