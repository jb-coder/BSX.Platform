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
/// results; access failures are recorded durably so lockout behaviour cannot be bypassed through
/// discovery or a rolled-back command (ADR-022).
/// </summary>
public sealed class CredentialAuthenticator
{
    // A fixed, valid PBKDF2 hash used to equalize login timing for unknown users (no enumeration).
    private static readonly PasswordHash DummyPasswordHash = PasswordHash.From(
        "210000.AAAAAAAAAAAAAAAAAAAAAA==.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
        "pbkdf2-sha256");

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccountSecurityStore _securityStore;
    private readonly IPostCommandActionQueue _postCommandActions;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="CredentialAuthenticator"/> class.</summary>
    public CredentialAuthenticator(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IAccountSecurityStore securityStore,
        IPostCommandActionQueue postCommandActions,
        TimeProvider timeProvider)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _securityStore = securityStore;
        _postCommandActions = postCommandActions;
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

        if (user.Status == UserStatus.Locked && user.IsLockedOut(nowUtc))
        {
            // Do not verify the password and do not extend the lockout.
            return Result.Failure<User>(IdentityApplicationErrors.InvalidCredentials);
        }

        if (user.Status is UserStatus.Disabled or UserStatus.Pending)
        {
            return Result.Failure<User>(IdentityApplicationErrors.InvalidCredentials);
        }

        if (user.Status == UserStatus.Locked)
        {
            // The lockout window elapsed. The unlock is persisted by the command on success; the
            // account security store performs the same transition when the password is wrong.
            user.Unlock();
        }

        PasswordCredential? credential = user.Credentials.OfType<PasswordCredential>().FirstOrDefault();
        if (credential is null || !_passwordHasher.Verify(password, credential.Hash))
        {
            // The command transaction rolls back failed results, so the failure is queued to run in
            // an independent scope after the transaction (ADR-022).
            _postCommandActions.Enqueue(ct => _securityStore.RecordAccessFailureAsync(user.Id, ct));
            return Result.Failure<User>(IdentityApplicationErrors.InvalidCredentials);
        }

        user.ResetAccessFailures();
        return Result.Success(user);
    }
}
