using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.Organizations.ListOrganizations;

/// <summary>Lists organizations within the active tenant.</summary>
/// <param name="Page">The one-based page number.</param>
/// <param name="Size">The page size.</param>
/// <param name="Search">An optional search term.</param>
[RequirePermission("crm.organizations.read")]
public sealed record ListOrganizationsQuery(int Page, int Size, string? Search) : IQuery<PagedResult<OrganizationDto>>;
