using BSX.BuildingBlocks.Persistence;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Application.Errors;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Users;
using BSX.Modules.Identity.Domain.Users.Credentials;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Application.Authentication;

/// <summary>
/// Shared credential verification for login and tenant discovery. Expected failures are returned as
/// results; access failures are recorded so lockout behaviour cannot be bypassed through discovery.
/// </summary>
public sealed class CredentialAuthenticator
{
    // A fixed, valid PBKDF2 hash used to equalize login timing for unknown users (no enumeration).
    private static readonly PasswordHash DummyPasswordHash = PasswordHash.From(
        "210000.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
        "pbkdf2-sha256");

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="CredentialAuthenticator"/> class.</summary>
    public CredentialAuthenticator(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <summary>Validates an email/password pair and returns the active user.</summary>
    public async Task<Result<User>> AuthenticateAsync(
        string? email,
        string password,
        CancellationToken cancellationToken = default)
    {
        Result<Email> emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<User>(IdentityApplicationErrors.InvalidCredentials);
        }

        User? user = await _users.GetByEmailAsync(emailResult.Value, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            _passwordHasher.Verify(password, DummyPasswordHash);
            return Result.Failure<User>(IdentityApplicationErrors.InvalidCredentials);
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();

        if (user.Status == UserStatus.Locked && user.LockoutEndUtc is { } lockoutEnd && nowUtc >= lockoutEnd)
        {
            user.Unlock();
        }

        if (user.Status is UserStatus.Disabled or UserStatus.Pending or UserStatus.Locked)
        {
            return Result.Failure<User>(IdentityApplicationErrors.InvalidCredentials);
        }

        PasswordCredential? credential = user.Credentials.OfType<PasswordCredential>().FirstOrDefault();
        if (credential is null || !_passwordHasher.Verify(password, credential.Hash))
        {
            user.RecordAccessFailure(nowUtc);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure<User>(IdentityApplicationErrors.InvalidCredentials);
        }

        user.ResetAccessFailures();
        return Result.Success(user);
    }
}
