using BSX.Contracts.Permissions;

namespace BSX.Modules.Identity.Application.Common;

/// <summary>Stable Identity permission codes published to the platform permission catalog (ADR-012).</summary>
public static class IdentityPermissionCodes
{
    /// <summary>Read users.</summary>
    public const string UsersRead = "identity.users.read";

    /// <summary>Create users.</summary>
    public const string UsersCreate = "identity.users.create";

    /// <summary>Assign roles and invite memberships.</summary>
    public const string UsersAssignRole = "identity.users.assign-role";

    /// <summary>Read roles.</summary>
    public const string RolesRead = "identity.roles.read";

    /// <summary>Create roles.</summary>
    public const string RolesCreate = "identity.roles.create";
}

/// <summary>
/// The Identity module's permission catalog. It is registered as a platform catalog source so
/// system roles stay correct as the permission set evolves (ADR-018).
/// </summary>
public sealed class IdentityPermissionCatalog : IPermissionCatalogSource
{
    /// <inheritdoc />
    public IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(IdentityPermissionCodes.UsersRead, "Read users."),
        new(IdentityPermissionCodes.UsersCreate, "Create users."),
        new(IdentityPermissionCodes.UsersAssignRole, "Assign roles and invite tenant memberships."),
        new(IdentityPermissionCodes.RolesRead, "Read roles."),
        new(IdentityPermissionCodes.RolesCreate, "Create roles."),
    ];
}
