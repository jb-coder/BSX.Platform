using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;
using BSX.Modules.Sales.Application.Dtos;

namespace BSX.Modules.Sales.Application.Opportunities.GetOpportunityById;

/// <summary>Gets an opportunity projection by identifier within the active tenant.</summary>
/// <param name="OpportunityId">The opportunity identifier.</param>
[RequirePermission(SalesPermissions.OpportunitiesRead)]
public sealed record GetOpportunityByIdQuery(Guid OpportunityId) : IQuery<OpportunityDto>;
