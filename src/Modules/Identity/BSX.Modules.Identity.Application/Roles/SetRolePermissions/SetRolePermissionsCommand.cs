using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Roles.SetRolePermissions;

/// <summary>Replaces a role's permissions.</summary>
/// <param name="RoleId">The role identifier.</param>
/// <param name="Permissions">The complete set of permission codes.</param>
public sealed record SetRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<string> Permissions) : ICommand;
