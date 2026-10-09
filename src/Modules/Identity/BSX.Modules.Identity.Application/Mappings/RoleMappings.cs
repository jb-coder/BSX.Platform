using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Domain.Roles;

namespace BSX.Modules.Identity.Application.Mappings;

/// <summary>Maps the <see cref="Role"/> aggregate to read-model DTOs.</summary>
public static class RoleMappings
{
    /// <summary>Maps a role to its projection.</summary>
    /// <param name="role">The role aggregate.</param>
    public static RoleDto ToDto(this Role role) => new(
        role.Id.Value,
        role.TenantId?.Value,
        role.Name.Value,
        role.Description,
        role.IsSystem,
        role.Permissions.Select(permission => permission.Code).ToArray());
}
