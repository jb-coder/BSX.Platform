using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.Sessions;
using BSX.Modules.Identity.Domain.Users;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication.CompletePasswordReset;

/// <summary>Handles <see cref="CompletePasswordResetCommand"/> (ADR-016).</summary>
public sealed class CompletePasswordResetCommandHandler : ICommandHandler<CompletePasswordResetCommand>
{
    private readonly IUserRepository _users;
    private readonly IUserSessionRepository _sessions;
    private readonly IPasswordResetTokenHasher _tokenHasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="CompletePasswordResetCommandHandler"/> class.</summary>
    public CompletePasswordResetCommandHandler(
        IUserRepository users,
        IUserSessionRepository sessions,
        IPasswordResetTokenHasher tokenHasher,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _sessions = sessions;
        _tokenHasher = tokenHasher;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> HandleAsync(CompletePasswordResetCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result.Failure(IdentityErrors.PasswordResetTokenInvalid);
        }

        var providedHash = _tokenHasher.Hash(request.Token);

        User? user = await _users.FindByPasswordResetTokenAsync(providedHash, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            // Do not reveal whether the token exists.
            return Result.Failure(IdentityErrors.PasswordResetTokenInvalid);
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        Result result = user.CompletePasswordReset(providedHash, _passwordHasher.Hash(request.NewPassword), nowUtc);

        if (result.IsFailure)
        {
            return result;
        }

        // A password reset invalidates every existing session (ADR-017).
        IReadOnlyCollection<UserSession> sessions = await _sessions
            .ListActiveByUserAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (UserSession session in sessions)
        {
            session.Revoke(nowUtc, "password_reset");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
