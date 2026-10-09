using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.Roles;

/// <summary>
/// A tenant-scoped collection of permissions. A null tenant denotes a platform/system role
/// (ADR-013).
/// </summary>
public sealed class Role : AggregateRoot<RoleId>
{
    private readonly HashSet<Permission> _permissions = [];

    private Role(RoleId id, TenantId? tenantId, RoleName name, string? description, bool isSystem)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        IsSystem = isSystem;
    }

    private Role()
    {
    }

    /// <summary>Gets the owning tenant; null for a platform/system role.</summary>
    public TenantId? TenantId { get; private set; }

    /// <summary>Gets the role name.</summary>
    public RoleName Name { get; private set; } = null!;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets a value indicating whether the role is a system role.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets a value indicating whether the role is soft-deleted.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Gets the granted permissions.</summary>
    public IReadOnlyCollection<Permission> Permissions => _permissions.ToArray();

    /// <summary>Creates a role.</summary>
    /// <param name="id">The role identifier.</param>
    /// <param name="tenantId">The owning tenant, or null for a system role.</param>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">The granted permissions.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="isSystem">Whether the role is a system role.</param>
    public static Result<Role> Create(
        RoleId id,
        TenantId? tenantId,
        RoleName name,
        IEnumerable<Permission> permissions,
        string? description = null,
        bool isSystem = false)
    {
        var role = new Role(id, tenantId, name, description, isSystem);

        foreach (Permission permission in permissions)
        {
            role._permissions.Add(permission);
        }

        role.RaiseDomainEvent(new RoleCreated(id, tenantId, name.Value));
        return Result.Success(role);
    }

    /// <summary>Renames the role. System roles are immutable.</summary>
    /// <param name="newName">The new role name.</param>
    public Result Rename(RoleName newName)
    {
        if (IsDeleted)
        {
            return Result.Failure(IdentityErrors.RoleAlreadyDeleted);
        }

        if (IsSystem)
        {
            return Result.Failure(IdentityErrors.RoleSystemImmutable);
        }

        if (Name == newName)
        {
            return Result.Success();
        }

        Name = newName;
        RaiseDomainEvent(new RoleRenamed(Id, newName.Value));
        return Result.Success();
    }

    /// <summary>Changes the role description. System roles are immutable.</summary>
    /// <param name="description">The new description.</param>
    public Result ChangeDescription(string? description)
    {
        if (IsDeleted)
        {
            return Result.Failure(IdentityErrors.RoleAlreadyDeleted);
        }

        if (IsSystem)
        {
            return Result.Failure(IdentityErrors.RoleSystemImmutable);
        }

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return Result.Success();
    }

    /// <summary>Grants a permission to the role.</summary>
    /// <param name="permission">The permission to grant.</param>
    public Result Grant(Permission permission)
    {
        if (IsDeleted)
        {
            return Result.Failure(IdentityErrors.RoleAlreadyDeleted);
        }

        if (!_permissions.Add(permission))
        {
            return Result.Failure(IdentityErrors.RolePermissionAlreadyGranted);
        }

        RaiseDomainEvent(new RolePermissionsChanged(Id, permission.Code, true));
        return Result.Success();
    }

    /// <summary>Revokes a permission from the role.</summary>
    /// <param name="permission">The permission to revoke.</param>
    public Result Revoke(Permission permission)
    {
        if (IsDeleted)
        {
            return Result.Failure(IdentityErrors.RoleAlreadyDeleted);
        }

        if (!_permissions.Remove(permission))
        {
            return Result.Failure(IdentityErrors.RolePermissionNotGranted);
        }

        RaiseDomainEvent(new RolePermissionsChanged(Id, permission.Code, false));
        return Result.Success();
    }

    /// <summary>Soft-deletes the role. System roles are immutable.</summary>
    public Result Delete()
    {
        if (IsDeleted)
        {
            return Result.Failure(IdentityErrors.RoleAlreadyDeleted);
        }

        if (IsSystem)
        {
            return Result.Failure(IdentityErrors.RoleSystemImmutable);
        }

        IsDeleted = true;
        RaiseDomainEvent(new RoleDeleted(Id));
        return Result.Success();
    }
}
