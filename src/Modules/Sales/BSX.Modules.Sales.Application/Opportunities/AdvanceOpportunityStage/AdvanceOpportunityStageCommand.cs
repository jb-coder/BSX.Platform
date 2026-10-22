using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;

namespace BSX.Modules.Sales.Application.Opportunities.AdvanceOpportunityStage;

/// <summary>Advances an open opportunity to the next sales stage.</summary>
/// <param name="OpportunityId">The opportunity identifier.</param>
[RequirePermission(SalesPermissions.OpportunitiesUpdate)]
public sealed record AdvanceOpportunityStageCommand(Guid OpportunityId) : ICommand;
