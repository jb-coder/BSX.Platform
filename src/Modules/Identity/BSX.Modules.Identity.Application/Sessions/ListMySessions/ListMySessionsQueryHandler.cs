using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Sessions.ListMySessions;

/// <summary>Handles <see cref="ListMySessionsQuery"/>.</summary>
public sealed class ListMySessionsQueryHandler : IQueryHandler<ListMySessionsQuery, IReadOnlyList<SessionDto>>
{
    private readonly IUserSessionRepository _sessions;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="ListMySessionsQueryHandler"/> class.</summary>
    public ListMySessionsQueryHandler(IUserSessionRepository sessions, ICurrentUser currentUser)
    {
        _sessions = sessions;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SessionDto>>> HandleAsync(ListMySessionsQuery request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<SessionDto>>(IdentityApplicationErrors.InvalidCredentials);
        }

        IReadOnlyCollection<UserSession> sessions = await _sessions
            .ListByUserAsync(UserId.From(userId), cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<SessionDto> dtos = sessions
            .OrderByDescending(session => session.CreatedOnUtc)
            .Select(session => new SessionDto(
                session.Id.Value,
                session.TenantId?.Value,
                session.Status.ToString(),
                session.CreatedOnUtc,
                session.LastRefreshedOnUtc,
                session.IdleExpiresOnUtc,
                session.Metadata?.UserAgent,
                session.Metadata?.IpAddress))
            .ToList();

        return Result.Success(dtos);
    }
}
