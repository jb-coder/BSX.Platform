using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;
using BSX.Modules.Sales.Application.Dtos;

namespace BSX.Modules.Sales.Application.Leads.GetLeadById;

/// <summary>Gets a lead projection by identifier within the active tenant.</summary>
/// <param name="LeadId">The lead identifier.</param>
[RequirePermission(SalesPermissions.LeadsRead)]
public sealed record GetLeadByIdQuery(Guid LeadId) : IQuery<LeadDto>;
