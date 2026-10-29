using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Memberships.RemoveMembership;

/// <summary>Removes a user's membership from the active workspace.</summary>
/// <param name="MembershipId">The membership identifier.</param>
[RequirePermission("identity.memberships.manage")]
public sealed record RemoveMembershipCommand(Guid MembershipId) : ICommand;
