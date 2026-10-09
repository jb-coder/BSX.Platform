using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Users.ListUsers;

/// <summary>Lists users with paging.</summary>
/// <param name="Page">The one-based page number.</param>
/// <param name="Size">The page size.</param>
/// <param name="Search">An optional search term.</param>
public sealed record ListUsersQuery(int Page, int Size, string? Search) : IQuery<PagedResult<UserSummaryDto>>;
