using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Roles.CreateRole;

/// <summary>Creates a tenant or system role.</summary>
/// <param name="TenantId">The owning tenant, or null for a system role.</param>
/// <param name="Name">The role name.</param>
/// <param name="Description">An optional description.</param>
/// <param name="Permissions">The granted permission codes.</param>
public sealed record CreateRoleCommand(
    Guid? TenantId,
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions) : ICommand<CreateRoleResponse>;
