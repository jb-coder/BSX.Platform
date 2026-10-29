using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Memberships.ChangeMembershipRole;

/// <summary>Replaces a membership's tenant roles with one optional role.</summary>
/// <param name="MembershipId">The membership identifier.</param>
/// <param name="RoleId">The replacement role, or null to remove all roles.</param>
[RequirePermission("identity.memberships.manage")]
public sealed record ChangeMembershipRoleCommand(Guid MembershipId, Guid? RoleId) : ICommand;
