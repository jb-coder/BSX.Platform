using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BSX.Modules.Identity.Infrastructure.Repositories;

internal sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly IdentityDbContext _context;

    public UserSessionRepository(IdentityDbContext context) => _context = context;

    public Task<UserSession?> GetByIdAsync(SessionId id, CancellationToken cancellationToken = default)
        => _context.Sessions
            .Include(session => session.RefreshTokens)
            .FirstOrDefaultAsync(session => session.Id == id, cancellationToken);

    public Task<UserSession?> FindByRefreshTokenAsync(RefreshTokenHash hash, CancellationToken cancellationToken = default)
        => _context.Sessions
            .Include(session => session.RefreshTokens)
            .FirstOrDefaultAsync(session => session.RefreshTokens.Any(token => token.Hash == hash), cancellationToken);

    public async Task<IReadOnlyCollection<UserSession>> ListActiveByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        List<UserSession> sessions = await _context.Sessions
            .Include(session => session.RefreshTokens)
            .Where(session => session.UserId == userId && session.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return sessions;
    }

    public async Task<IReadOnlyCollection<UserSession>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        List<UserSession> sessions = await _context.Sessions
            .Include(session => session.RefreshTokens)
            .Where(session => session.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return sessions;
    }

    public async Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
        => await _context.Sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
}
