using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Memberships.AssignRoleToMembership;

/// <summary>Assigns a tenant role to a membership.</summary>
/// <param name="MembershipId">The membership identifier.</param>
/// <param name="RoleId">The role identifier.</param>
public sealed record AssignRoleToMembershipCommand(Guid MembershipId, Guid RoleId) : ICommand;
