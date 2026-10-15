using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Memberships.InviteUserToTenant;

/// <summary>Invites a user to a tenant.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="InvitedByUserId">The inviter, if any.</param>
[RequirePermission("identity.users.assign-role")]
public sealed record InviteUserToTenantCommand(Guid TenantId, Guid UserId, Guid? InvitedByUserId) : ICommand<InviteUserToTenantResponse>;
