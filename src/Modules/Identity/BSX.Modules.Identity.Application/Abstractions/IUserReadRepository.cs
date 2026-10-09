using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Read-model access for users (CQRS read side). Implemented in Infrastructure.</summary>
public interface IUserReadRepository
{
    /// <summary>Searches users with paging.</summary>
    /// <param name="page">The one-based page number.</param>
    /// <param name="size">The page size.</param>
    /// <param name="search">An optional search term.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedResult<UserSummaryDto>> SearchAsync(int page, int size, string? search, CancellationToken cancellationToken = default);
}
