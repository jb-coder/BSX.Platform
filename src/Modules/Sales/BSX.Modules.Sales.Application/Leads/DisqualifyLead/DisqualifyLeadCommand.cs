using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;

namespace BSX.Modules.Sales.Application.Leads.DisqualifyLead;

/// <summary>Disqualifies a new or contacted lead.</summary>
/// <param name="LeadId">The lead identifier.</param>
[RequirePermission(SalesPermissions.LeadsUpdate)]
public sealed record DisqualifyLeadCommand(Guid LeadId) : ICommand;
