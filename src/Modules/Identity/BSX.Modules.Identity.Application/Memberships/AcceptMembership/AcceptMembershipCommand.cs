using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.Memberships.AcceptMembership;

/// <summary>Accepts a pending tenant membership.</summary>
/// <param name="MembershipId">The membership identifier.</param>
public sealed record AcceptMembershipCommand(Guid MembershipId) : ICommand;
