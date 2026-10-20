using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.Organizations.GetOrganizationById;

/// <summary>Gets an organization projection by identifier within the active tenant.</summary>
/// <param name="OrganizationId">The organization identifier.</param>
[RequirePermission("crm.organizations.read")]
public sealed record GetOrganizationByIdQuery(Guid OrganizationId) : IQuery<OrganizationDto>;
