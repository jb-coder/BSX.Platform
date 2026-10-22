using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;

namespace BSX.Modules.Sales.Application.Leads.MarkLeadContacted;

/// <summary>Marks a new lead as contacted.</summary>
/// <param name="LeadId">The lead identifier.</param>
[RequirePermission(SalesPermissions.LeadsUpdate)]
public sealed record MarkLeadContactedCommand(Guid LeadId) : ICommand;
