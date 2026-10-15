using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Sessions.RevokeSession;

/// <summary>Handles <see cref="RevokeSessionCommand"/>.</summary>
public sealed class RevokeSessionCommandHandler : ICommandHandler<RevokeSessionCommand>
{
    private readonly IUserSessionRepository _sessions;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="RevokeSessionCommandHandler"/> class.</summary>
    public RevokeSessionCommandHandler(
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
    public async Task<Result> HandleAsync(RevokeSessionCommand request, CancellationToken cancellationToken = default)
    {
        if (request.SessionId == Guid.Empty)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidCredentials);
        }

        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(IdentityApplicationErrors.InvalidCredentials);
        }

        UserSession? session = await _sessions.GetByIdAsync(SessionId.From(request.SessionId), cancellationToken).ConfigureAwait(false);

        if (session is null)
        {
            return Result.Failure(IdentityErrorsSessionNotFound());
        }

        if (session.UserId.Value != userId)
        {
            return Result.Failure(Error.Forbidden("Identity.Session.Forbidden", "The session does not belong to the current user."));
        }

        session.Revoke(_timeProvider.GetUtcNow(), "revoked_by_user");
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static Error IdentityErrorsSessionNotFound()
        => Error.NotFound("Identity.Session.NotFound", "The session was not found.");
}
