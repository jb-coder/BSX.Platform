using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication.Logout;

/// <summary>Handles <see cref="LogoutCommand"/>.</summary>
public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IUserSessionRepository _sessions;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="LogoutCommandHandler"/> class.</summary>
    public LogoutCommandHandler(
        IUserSessionRepository sessions,
        IRefreshTokenHasher refreshTokenHasher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions;
        _refreshTokenHasher = refreshTokenHasher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(LogoutCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Success();
        }

        UserSession? session = await _sessions
            .FindByRefreshTokenAsync(_refreshTokenHasher.Hash(request.RefreshToken), cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Result.Success();
        }

        session.Revoke(_timeProvider.GetUtcNow(), "logout");
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
