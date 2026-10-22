using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;

namespace BSX.Modules.Sales.Application.Opportunities.MarkOpportunityWon;

/// <summary>Marks a negotiation as won.</summary>
/// <param name="OpportunityId">The opportunity identifier.</param>
[RequirePermission(SalesPermissions.OpportunitiesUpdate)]
public sealed record MarkOpportunityWonCommand(Guid OpportunityId) : ICommand;
