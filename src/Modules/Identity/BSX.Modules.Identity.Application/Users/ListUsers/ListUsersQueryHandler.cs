using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Users.ListUsers;

/// <summary>Handles <see cref="ListUsersQuery"/>.</summary>
public sealed class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, PagedResult<UserSummaryDto>>
{
    private const int MaxSize = 100;
    private const int DefaultSize = 20;

    private readonly IUserReadRepository _users;

    /// <summary>Initializes a new instance of the <see cref="ListUsersQueryHandler"/> class.</summary>
    public ListUsersQueryHandler(IUserReadRepository users) => _users = users;

    /// <inheritdoc />
    public async Task<Result<PagedResult<UserSummaryDto>>> HandleAsync(ListUsersQuery request, CancellationToken cancellationToken = default)
    {
        int page = request.Page < 1 ? 1 : request.Page;
        int size = request.Size is < 1 or > MaxSize ? DefaultSize : request.Size;

        PagedResult<UserSummaryDto> result = await _users
            .SearchAsync(page, size, request.Search, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(result);
    }
}
