using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Errors;

/// <summary>Stable error catalog for the Identity application layer.</summary>
public static class IdentityApplicationErrors
{
    /// <summary>The user was not found.</summary>
    public static Error UserNotFound => Error.NotFound("Identity.User.NotFound", "The user was not found.");

    /// <summary>A user with the given email already exists.</summary>
    public static Error UserEmailAlreadyExists => Error.Conflict("Identity.User.EmailAlreadyExists", "A user with this email already exists.");

    /// <summary>The role was not found.</summary>
    public static Error RoleNotFound => Error.NotFound("Identity.Role.NotFound", "The role was not found.");

    /// <summary>A role with the given name already exists in the tenant.</summary>
    public static Error RoleNameAlreadyExists => Error.Conflict("Identity.Role.NameAlreadyExists", "A role with this name already exists in the tenant.");

    /// <summary>The membership was not found.</summary>
    public static Error MembershipNotFound => Error.NotFound("Identity.Membership.NotFound", "The membership was not found.");

    /// <summary>The user already belongs to the tenant.</summary>
    public static Error MembershipAlreadyExists => Error.Conflict("Identity.Membership.AlreadyExists", "The user already belongs to this tenant.");

    /// <summary>The service account was not found.</summary>
    public static Error ServiceAccountNotFound => Error.NotFound("Identity.ServiceAccount.NotFound", "The service account was not found.");

    /// <summary>The API key was not found.</summary>
    public static Error ApiKeyNotFound => Error.NotFound("Identity.ApiKey.NotFound", "The API key was not found.");

    /// <summary>The role does not belong to the membership's tenant.</summary>
    public static Error RoleTenantMismatch => Error.Conflict("Identity.Role.TenantMismatch", "The role does not belong to the membership's tenant.");

    /// <summary>The user identifier is invalid.</summary>
    public static Error InvalidUserId => Error.Validation("Identity.User.InvalidId", "The user identifier is invalid.");

    /// <summary>The role identifier is invalid.</summary>
    public static Error InvalidRoleId => Error.Validation("Identity.Role.InvalidId", "The role identifier is invalid.");

    /// <summary>The tenant identifier is invalid.</summary>
    public static Error InvalidTenantId => Error.Validation("Identity.Tenant.InvalidId", "The tenant identifier is invalid.");

    /// <summary>The membership identifier is invalid.</summary>
    public static Error InvalidMembershipId => Error.Validation("Identity.Membership.InvalidId", "The membership identifier is invalid.");

    /// <summary>The service account identifier is invalid.</summary>
    public static Error InvalidServiceAccountId => Error.Validation("Identity.ServiceAccount.InvalidId", "The service account identifier is invalid.");

    /// <summary>The API key identifier is invalid.</summary>
    public static Error InvalidApiKeyId => Error.Validation("Identity.ApiKey.InvalidId", "The API key identifier is invalid.");
}
