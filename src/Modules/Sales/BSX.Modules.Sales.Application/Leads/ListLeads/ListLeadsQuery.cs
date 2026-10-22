using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;
using BSX.Modules.Sales.Application.Dtos;

namespace BSX.Modules.Sales.Application.Leads.ListLeads;

/// <summary>Lists lead summaries within the active tenant.</summary>
/// <param name="Page">The one-based page number.</param>
/// <param name="Size">The page size.</param>
/// <param name="Search">An optional title search term.</param>
[RequirePermission(SalesPermissions.LeadsRead)]
public sealed record ListLeadsQuery(int Page, int Size, string? Search) : IQuery<PagedResult<LeadSummaryDto>>;
