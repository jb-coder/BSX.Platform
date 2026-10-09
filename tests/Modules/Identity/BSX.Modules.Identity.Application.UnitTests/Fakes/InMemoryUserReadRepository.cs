using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

/// <summary>In-memory <see cref="IUserReadRepository"/> for query tests.</summary>
internal sealed class InMemoryUserReadRepository : IUserReadRepository
{
    private readonly List<UserSummaryDto> _users = [];

    public void Seed(params UserSummaryDto[] users) => _users.AddRange(users);

    public Task<PagedResult<UserSummaryDto>> SearchAsync(int page, int size, string? search, CancellationToken cancellationToken = default)
    {
        List<UserSummaryDto> filtered = string.IsNullOrWhiteSpace(search)
            ? [.. _users]
            : _users.Where(user =>
                user.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                || user.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        List<UserSummaryDto> items = filtered.Skip((page - 1) * size).Take(size).ToList();
        return Task.FromResult(new PagedResult<UserSummaryDto>(items, page, size, filtered.Count));
    }
}
