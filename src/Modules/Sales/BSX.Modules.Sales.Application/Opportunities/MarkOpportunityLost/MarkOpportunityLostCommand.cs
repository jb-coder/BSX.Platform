using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;

namespace BSX.Modules.Sales.Application.Opportunities.MarkOpportunityLost;

/// <summary>Marks an open opportunity as lost.</summary>
/// <param name="OpportunityId">The opportunity identifier.</param>
[RequirePermission(SalesPermissions.OpportunitiesUpdate)]
public sealed record MarkOpportunityLostCommand(Guid OpportunityId) : ICommand;
