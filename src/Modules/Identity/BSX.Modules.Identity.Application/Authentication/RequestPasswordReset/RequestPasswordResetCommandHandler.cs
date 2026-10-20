using BSX.BuildingBlocks.Cqrs;
using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication.RequestPasswordReset;

/// <summary>Handles <see cref="RequestPasswordResetCommand"/> (ADR-016).</summary>
public sealed class RequestPasswordResetCommandHandler : ICommandHandler<RequestPasswordResetCommand, RequestPasswordResetResponse>
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenHasher _tokenHasher;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IPasswordResetNotifier _notifier;
    private readonly IRateLimiter _rateLimiter;
    private readonly RateLimitOptions _rateLimitOptions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="RequestPasswordResetCommandHandler"/> class.</summary>
    public RequestPasswordResetCommandHandler(
        IUserRepository users,
        IPasswordResetTokenHasher tokenHasher,
        ISecureTokenGenerator tokenGenerator,
        IPasswordResetNotifier notifier,
        IRateLimiter rateLimiter,
        RateLimitOptions rateLimitOptions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _tokenHasher = tokenHasher;
        _tokenGenerator = tokenGenerator;
        _notifier = notifier;
        _rateLimiter = rateLimiter;
        _rateLimitOptions = rateLimitOptions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<RequestPasswordResetResponse>> HandleAsync(RequestPasswordResetCommand request, CancellationToken cancellationToken = default)
    {
        string partitionKey = $"password-reset:{request.Email?.Trim().ToLowerInvariant()}";
        if (!_rateLimiter.Acquire(partitionKey, _rateLimitOptions.PasswordReset).IsAllowed)
        {
            return Result.Failure<RequestPasswordResetResponse>(IdentityApplicationErrors.TooManyRequests);
        }

        // Fail explicitly (and enumeration-safely) when the deployment cannot deliver reset emails,
        // instead of reporting a success that could never reach the user (ADR-016).
        if (!_notifier.IsDeliveryEnabled)
        {
            return Result.Failure<RequestPasswordResetResponse>(IdentityApplicationErrors.PasswordResetUnavailable);
        }

        // Enumeration-safe: identical response regardless of account existence or validity.
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Success(new RequestPasswordResetResponse());
        }

        User? user = await _users.GetByEmailAsync(emailResult.Value, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Success(new RequestPasswordResetResponse());
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        string token = _tokenGenerator.GenerateToken();

        // Persist only the hash; the previous token is replaced.
        user.RequestPasswordReset(
            PasswordResetTokenId.New(),
            _tokenHasher.Hash(token),
            nowUtc,
            ResetTokenLifetime);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Deliver out of band with the plaintext token; nothing secret is persisted.
        await _notifier.NotifyAsync(user.Email.Value, token, nowUtc.Add(ResetTokenLifetime), cancellationToken).ConfigureAwait(false);

        return Result.Success(new RequestPasswordResetResponse());
    }
}
