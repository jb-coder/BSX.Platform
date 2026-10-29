using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Memberships.ListTenantMemberships;

/// <summary>Lists the memberships of the active workspace.</summary>
[RequirePermission("identity.memberships.read")]
public sealed record ListTenantMembershipsQuery : IQuery<IReadOnlyList<MembershipDto>>;
