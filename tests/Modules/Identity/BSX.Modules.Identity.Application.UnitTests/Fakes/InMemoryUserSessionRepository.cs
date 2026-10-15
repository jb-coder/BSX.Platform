using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

internal sealed class InMemoryUserSessionRepository : IUserSessionRepository
{
    private readonly List<UserSession> _sessions = [];

    public IReadOnlyCollection<UserSession> Sessions => _sessions;

    public Task<UserSession?> GetByIdAsync(SessionId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_sessions.Find(session => session.Id == id));

    public Task<UserSession?> FindByRefreshTokenAsync(RefreshTokenHash hash, CancellationToken cancellationToken = default)
        => Task.FromResult(_sessions.Find(session => session.RefreshTokens.Any(token => token.Hash == hash)));

    public Task<IReadOnlyCollection<UserSession>> ListActiveByUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<UserSession>>(
            _sessions.Where(session => session.UserId == userId && session.Status == SessionStatus.Active).ToList());

    public Task<IReadOnlyCollection<UserSession>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyCollection<UserSession>>(
            _sessions.Where(session => session.UserId == userId).ToList());

    public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _sessions.Add(session);
        return Task.CompletedTask;
    }
}
