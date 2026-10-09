using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Repositories;

internal sealed class UserReadRepository : IUserReadRepository
{
    private readonly IdentityDbContext _context;

    public UserReadRepository(IdentityDbContext context) => _context = context;

    public async Task<PagedResult<UserSummaryDto>> SearchAsync(
        int page,
        int size,
        string? search,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim()}%";
            query = query.Where(user =>
                EF.Functions.Like(EF.Property<string>(user, "SearchEmail"), pattern)
                || EF.Functions.Like(EF.Property<string>(user, "SearchName"), pattern));
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<User> users = await query
            .OrderBy(user => user.AccessFailedCount)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<UserSummaryDto> items = users
            .Select(user => new UserSummaryDto(user.Id.Value, user.Email.Value, user.Name.Value, user.Status.ToString()))
            .ToList();

        return new PagedResult<UserSummaryDto>(items, page, size, total);
    }
}
