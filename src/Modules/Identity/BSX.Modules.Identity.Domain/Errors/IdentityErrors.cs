using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.Errors;

/// <summary>
/// Stable, namespaced error catalog for the Identity domain (ADR-011 code convention).
/// </summary>
public static class IdentityErrors
{
    /// <summary>Email value is invalid.</summary>
    public static Error EmailInvalid => Error.Validation("Identity.User.Email.Invalid", "The email address is invalid.");

    /// <summary>Display name value is invalid.</summary>
    public static Error NameInvalid => Error.Validation("Identity.User.Name.Invalid", "The display name is invalid.");

    /// <summary>Role name value is invalid.</summary>
    public static Error RoleNameInvalid => Error.Validation("Identity.Role.Name.Invalid", "The role name is invalid.");

    /// <summary>Permission code is invalid.</summary>
    public static Error PermissionInvalid => Error.Validation("Identity.Permission.Invalid", "The permission code is invalid.");

    /// <summary>The user is already active.</summary>
    public static Error UserAlreadyActive => Error.Conflict("Identity.User.AlreadyActive", "The user is already active.");

    /// <summary>The user is already deactivated.</summary>
    public static Error UserAlreadyDeactivated => Error.Conflict("Identity.User.AlreadyDeactivated", "The user is already deactivated.");

    /// <summary>An active user requires at least one credential.</summary>
    public static Error UserCredentialRequired => Error.Validation("Identity.User.CredentialRequired", "An active user must have at least one credential.");

    /// <summary>The user is not locked.</summary>
    public static Error UserNotLocked => Error.Conflict("Identity.User.NotLocked", "The user is not locked.");

    /// <summary>The lockout end is invalid.</summary>
    public static Error UserLockoutInvalid => Error.Validation("Identity.User.LockoutInvalid", "The lockout end must be in the future.");

    /// <summary>The user has no password credential.</summary>
    public static Error UserNoPasswordCredential => Error.Conflict("Identity.User.NoPasswordCredential", "The user has no password credential.");

    /// <summary>A credential of the given type already exists.</summary>
    public static Error CredentialAlreadyExists => Error.Conflict("Identity.User.CredentialAlreadyExists", "A credential of this type already exists.");

    /// <summary>The external provider is already linked.</summary>
    public static Error ExternalProviderAlreadyLinked => Error.Conflict("Identity.User.ExternalProviderAlreadyLinked", "The external provider is already linked to this user.");

    /// <summary>The credential was not found.</summary>
    public static Error CredentialNotFound => Error.NotFound("Identity.User.CredentialNotFound", "The credential was not found.");

    /// <summary>The last credential cannot be removed from an active user.</summary>
    public static Error LastCredentialCannotBeRemoved => Error.Conflict("Identity.User.LastCredential", "An active user must keep at least one credential.");

    /// <summary>The password reset token is invalid.</summary>
    public static Error PasswordResetTokenInvalid => Error.Failure("Identity.User.PasswordResetTokenInvalid", "The password reset token is invalid.");

    /// <summary>The password reset token has expired.</summary>
    public static Error PasswordResetTokenExpired => Error.Failure("Identity.User.PasswordResetTokenExpired", "The password reset token has expired.");

    /// <summary>The password reset token was already used.</summary>
    public static Error PasswordResetTokenUsed => Error.Conflict("Identity.User.PasswordResetTokenUsed", "The password reset token has already been used.");

    /// <summary>The password reset token does not exist.</summary>
    public static Error PasswordResetTokenMissing => Error.Conflict("Identity.User.PasswordResetTokenMissing", "No password reset was requested.");

    /// <summary>The recovery code is invalid.</summary>
    public static Error RecoveryCodeInvalid => Error.Failure("Identity.User.RecoveryCodeInvalid", "The recovery code is invalid.");

    /// <summary>The recovery code was already used.</summary>
    public static Error RecoveryCodeUsed => Error.Conflict("Identity.User.RecoveryCodeUsed", "The recovery code has already been used.");

    /// <summary>MFA is not enabled.</summary>
    public static Error MfaNotEnabled => Error.Conflict("Identity.User.MfaNotEnabled", "Multi-factor authentication is not enabled.");

    /// <summary>MFA is already enabled.</summary>
    public static Error MfaAlreadyEnabled => Error.Conflict("Identity.User.MfaAlreadyEnabled", "Multi-factor authentication is already enabled.");

    /// <summary>The membership status transition is invalid.</summary>
    public static Error MembershipInvalidTransition => Error.Conflict("Identity.Membership.InvalidTransition", "The membership status transition is invalid.");

    /// <summary>The role is already assigned to the membership.</summary>
    public static Error MembershipRoleAlreadyAssigned => Error.Conflict("Identity.Membership.RoleAlreadyAssigned", "The role is already assigned.");

    /// <summary>The role is not assigned to the membership.</summary>
    public static Error MembershipRoleNotAssigned => Error.NotFound("Identity.Membership.RoleNotAssigned", "The role is not assigned.");

    /// <summary>A system role cannot be modified.</summary>
    public static Error RoleSystemImmutable => Error.Conflict("Identity.Role.SystemImmutable", "A system role cannot be modified.");

    /// <summary>The role is already deleted.</summary>
    public static Error RoleAlreadyDeleted => Error.Conflict("Identity.Role.AlreadyDeleted", "The role is already deleted.");

    /// <summary>The permission is already granted to the role.</summary>
    public static Error RolePermissionAlreadyGranted => Error.Conflict("Identity.Role.PermissionAlreadyGranted", "The permission is already granted to the role.");

    /// <summary>The permission is not granted to the role.</summary>
    public static Error RolePermissionNotGranted => Error.NotFound("Identity.Role.PermissionNotGranted", "The permission is not granted to the role.");

    /// <summary>The role is already assigned to the service account.</summary>
    public static Error ServiceAccountRoleAlreadyAssigned => Error.Conflict("Identity.ServiceAccount.RoleAlreadyAssigned", "The role is already assigned to the service account.");

    /// <summary>The role is not assigned to the service account.</summary>
    public static Error ServiceAccountRoleNotAssigned => Error.NotFound("Identity.ServiceAccount.RoleNotAssigned", "The role is not assigned to the service account.");

    /// <summary>The service account is already enabled.</summary>
    public static Error ServiceAccountAlreadyEnabled => Error.Conflict("Identity.ServiceAccount.AlreadyEnabled", "The service account is already enabled.");

    /// <summary>The service account is already disabled.</summary>
    public static Error ServiceAccountAlreadyDisabled => Error.Conflict("Identity.ServiceAccount.AlreadyDisabled", "The service account is already disabled.");

    /// <summary>The API key is revoked.</summary>
    public static Error ApiKeyRevoked => Error.Conflict("Identity.ApiKey.Revoked", "The API key is revoked.");

    /// <summary>The API key is already revoked.</summary>
    public static Error ApiKeyAlreadyRevoked => Error.Conflict("Identity.ApiKey.AlreadyRevoked", "The API key is already revoked.");

    /// <summary>The API key has expired.</summary>
    public static Error ApiKeyExpired => Error.Conflict("Identity.ApiKey.Expired", "The API key has expired.");

    /// <summary>The API key scope is already present.</summary>
    public static Error ApiKeyScopeAlreadyPresent => Error.Conflict("Identity.ApiKey.ScopeAlreadyPresent", "The scope is already present on the API key.");

    /// <summary>The API key scope is not present.</summary>
    public static Error ApiKeyScopeNotPresent => Error.NotFound("Identity.ApiKey.ScopeNotPresent", "The scope is not present on the API key.");

    /// <summary>The API key expiry is invalid.</summary>
    public static Error ApiKeyExpiryInvalid => Error.Validation("Identity.ApiKey.ExpiryInvalid", "The API key expiry must be in the future.");
}
