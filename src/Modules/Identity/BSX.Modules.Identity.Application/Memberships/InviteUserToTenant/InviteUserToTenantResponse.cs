namespace BSX.Modules.Identity.Application.Memberships.InviteUserToTenant;

/// <summary>The result of inviting a user to a tenant.</summary>
/// <param name="MembershipId">The new membership identifier.</param>
public sealed record InviteUserToTenantResponse(Guid MembershipId);
