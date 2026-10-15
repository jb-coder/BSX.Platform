using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication.LogoutAll;

/// <summary>Handles <see cref="LogoutAllCommand"/>.</summary>
public sealed class LogoutAllCommandHandler : ICommandHandler<LogoutAllCommand>
{
    private readonly IUserSessionRepository _sessions;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="LogoutAllCommandHandler"/> class.</summary>
    public LogoutAllCommandHandler(
        IUserSessionRepository sessions,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(LogoutAllCommand request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not { } userId || userId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidCredentials);
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        IReadOnlyCollection<UserSession> sessions = await _sessions
            .ListActiveByUserAsync(UserId.From(userId), cancellationToken)
            .ConfigureAwait(false);

        foreach (UserSession session in sessions)
        {
            session.Revoke(nowUtc, "logout_all");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
