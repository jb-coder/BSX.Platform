using BSX.Modules.Identity.Domain.DomainEvents;
using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.Errors;
using BSX.Modules.Identity.Domain.Primitives;
using BSX.Modules.Identity.Domain.Users.Credentials;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;
using BSX.SharedKernel.Results;

namespace BSX.Modules.Identity.Domain.Users;

/// <summary>
/// The global identity of a person who can access the platform (ADR-013).
/// Owns the user's authentication credentials as child entities (ADR-014).
/// </summary>
public sealed class User : AggregateRoot<UserId>, IPrincipal
{
    /// <summary>Default number of failed access attempts before lockout.</summary>
    public const int DefaultMaxAccessFailedCount = 5;

    /// <summary>Default lockout duration.</summary>
    public static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<Credential> _credentials = [];
    private readonly HashSet<RoleId> _platformRoleIds = [];

    private User(UserId id, Email email, PersonName name, DateTimeOffset registeredOnUtc)
        : base(id)
    {
        Email = email;
        Name = name;
        RegisteredOnUtc = registeredOnUtc;
        Status = UserStatus.Pending;
        SecurityStamp = SecurityStamp.New();
    }

    private User()
    {
    }

    /// <summary>Gets the globally unique email address.</summary>
    public Email Email { get; private set; } = null!;

    /// <summary>Gets the display name.</summary>
    public PersonName Name { get; private set; } = null!;

    /// <summary>Gets the lifecycle status.</summary>
    public UserStatus Status { get; private set; }

    /// <summary>Gets the security stamp validated at refresh time (ADR-017).</summary>
    public SecurityStamp SecurityStamp { get; private set; } = null!;

    /// <summary>Gets the registration timestamp.</summary>
    public DateTimeOffset RegisteredOnUtc { get; private set; }

    /// <summary>Gets the consecutive failed access count.</summary>
    public int AccessFailedCount { get; private set; }

    /// <summary>Gets the lockout expiry, if locked.</summary>
    public DateTimeOffset? LockoutEndUtc { get; private set; }

    /// <summary>Gets the active password reset token, if any (ADR-016).</summary>
    public PasswordResetToken? PasswordResetToken { get; private set; }

    /// <summary>Gets the owned credentials (ADR-014).</summary>
    public IReadOnlyCollection<Credential> Credentials => _credentials.AsReadOnly();

    /// <summary>Gets the platform (system) roles assigned to the user.</summary>
    public IReadOnlyCollection<RoleId> PlatformRoleIds => _platformRoleIds.ToArray();

    /// <inheritdoc />
    public PrincipalId Principal => PrincipalId.From(Id.Value);

    /// <summary>Determines whether the user has a credential of the given type.</summary>
    /// <param name="type">The credential type.</param>
    public bool HasCredential(CredentialType type) => _credentials.Exists(credential => credential.Type == type);

    /// <summary>Determines whether the user has the given credential.</summary>
    /// <param name="credentialId">The credential identifier.</param>
    public bool HasCredential(CredentialId credentialId) => _credentials.Exists(credential => credential.Id == credentialId);

    /// <summary>Gets a value indicating whether the user has any credential.</summary>
    public bool HasAnyCredential => _credentials.Count > 0;

    /// <summary>Gets a value indicating whether multi-factor authentication is enabled.</summary>
    public bool IsMfaEnabled => HasCredential(CredentialType.Totp) || HasCredential(CredentialType.WebAuthn);

    /// <summary>Registers a new user in the <see cref="UserStatus.Pending"/> state.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="email">The email address.</param>
    /// <param name="name">The display name.</param>
    /// <param name="registeredOnUtc">The registration timestamp.</param>
    public static Result<User> Register(UserId id, Email email, PersonName name, DateTimeOffset registeredOnUtc)
    {
        var user = new User(id, email, name, registeredOnUtc);
        user.RaiseDomainEvent(new UserRegistered(id, email.Value));
        return Result.Success(user);
    }

    /// <summary>Activates the user. Requires at least one credential.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result Activate(DateTimeOffset nowUtc)
    {
        _ = nowUtc;

        if (Status == UserStatus.Active)
        {
            return Result.Failure(IdentityErrors.UserAlreadyActive);
        }

        if (!HasAnyCredential)
        {
            return Result.Failure(IdentityErrors.UserCredentialRequired);
        }

        Status = UserStatus.Active;
        LockoutEndUtc = null;
        AccessFailedCount = 0;
        RotateSecurityStamp();
        RaiseDomainEvent(new UserActivated(Id));
        return Result.Success();
    }

    /// <summary>Deactivates the user.</summary>
    public Result Deactivate()
    {
        if (Status == UserStatus.Disabled)
        {
            return Result.Failure(IdentityErrors.UserAlreadyDeactivated);
        }

        Status = UserStatus.Disabled;
        RotateSecurityStamp();
        RaiseDomainEvent(new UserDeactivated(Id));
        return Result.Success();
    }

    /// <summary>Locks the user until the given time.</summary>
    /// <param name="untilUtc">The lockout expiry.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result Lock(DateTimeOffset untilUtc, DateTimeOffset nowUtc)
    {
        if (untilUtc <= nowUtc)
        {
            return Result.Failure(IdentityErrors.UserLockoutInvalid);
        }

        Status = UserStatus.Locked;
        LockoutEndUtc = untilUtc;
        RaiseDomainEvent(new UserLocked(Id, untilUtc));
        return Result.Success();
    }

    /// <summary>Unlocks the user.</summary>
    public Result Unlock()
    {
        if (Status != UserStatus.Locked)
        {
            return Result.Failure(IdentityErrors.UserNotLocked);
        }

        Status = HasAnyCredential ? UserStatus.Active : UserStatus.Pending;
        LockoutEndUtc = null;
        AccessFailedCount = 0;
        RaiseDomainEvent(new UserUnlocked(Id));
        return Result.Success();
    }

    /// <summary>Records a failed access attempt and locks the user when the threshold is reached.</summary>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="maxAttempts">The failure threshold.</param>
    /// <param name="lockoutDuration">The lockout duration; defaults to <see cref="DefaultLockoutDuration"/>.</param>
    public Result RecordAccessFailure(DateTimeOffset nowUtc, int maxAttempts = DefaultMaxAccessFailedCount, TimeSpan? lockoutDuration = null)
    {
        AccessFailedCount++;

        if (AccessFailedCount >= maxAttempts)
        {
            TimeSpan duration = lockoutDuration ?? DefaultLockoutDuration;
            Status = UserStatus.Locked;
            LockoutEndUtc = nowUtc.Add(duration);
            RaiseDomainEvent(new UserLocked(Id, LockoutEndUtc.Value));
        }

        return Result.Success();
    }

    /// <summary>Resets the failed access counter.</summary>
    public void ResetAccessFailures() => AccessFailedCount = 0;

    /// <summary>Changes the email address.</summary>
    /// <param name="newEmail">The new email address.</param>
    public Result ChangeEmail(Email newEmail)
    {
        if (Email == newEmail)
        {
            return Result.Success();
        }

        Email = newEmail;
        RotateSecurityStamp();
        RaiseDomainEvent(new UserEmailChanged(Id, newEmail.Value));
        return Result.Success();
    }

    /// <summary>Changes the display name.</summary>
    /// <param name="newName">The new display name.</param>
    public Result ChangeName(PersonName newName)
    {
        if (Name == newName)
        {
            return Result.Success();
        }

        Name = newName;
        RaiseDomainEvent(new UserNameChanged(Id, newName.Value));
        return Result.Success();
    }

    /// <summary>Sets or replaces the password credential.</summary>
    /// <param name="hash">The new password hash.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result SetPassword(PasswordHash hash, DateTimeOffset nowUtc)
    {
        PasswordCredential? existing = _credentials.OfType<PasswordCredential>().FirstOrDefault();

        if (existing is null)
        {
            _credentials.Add(new PasswordCredential(CredentialId.New(), hash, nowUtc));
            RaiseDomainEvent(new UserCredentialAdded(Id, CredentialType.Password));
        }
        else
        {
            existing.Update(hash);
        }

        RotateSecurityStamp();
        RaiseDomainEvent(new UserPasswordChanged(Id));
        return Result.Success();
    }

    /// <summary>Changes the password when a password credential already exists.</summary>
    /// <param name="hash">The new password hash.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result ChangePassword(PasswordHash hash, DateTimeOffset nowUtc)
        => HasCredential(CredentialType.Password)
            ? SetPassword(hash, nowUtc)
            : Result.Failure(IdentityErrors.UserNoPasswordCredential);

    /// <summary>Links an external identity provider.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="subjectId">The provider subject identifier.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result AddExternalCredential(ExternalProvider provider, string subjectId, DateTimeOffset nowUtc)
    {
        bool alreadyLinked = _credentials
            .OfType<ExternalProviderCredential>()
            .Any(credential => credential.Provider == provider && string.Equals(credential.SubjectId, subjectId, StringComparison.Ordinal));

        if (alreadyLinked)
        {
            return Result.Failure(IdentityErrors.ExternalProviderAlreadyLinked);
        }

        _credentials.Add(new ExternalProviderCredential(CredentialId.New(), provider, subjectId, nowUtc));
        RaiseDomainEvent(new UserCredentialAdded(Id, CredentialType.ExternalProvider));
        return Result.Success();
    }

    /// <summary>Adds a WebAuthn (passkey) credential.</summary>
    /// <param name="credentialIdentifier">The authenticator credential identifier.</param>
    /// <param name="publicKey">The public key.</param>
    /// <param name="name">An optional label.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result AddPasskey(string credentialIdentifier, byte[] publicKey, string? name, DateTimeOffset nowUtc)
    {
        _credentials.Add(new WebAuthnCredential(CredentialId.New(), credentialIdentifier, publicKey, 0, name, nowUtc));
        RaiseDomainEvent(new UserCredentialAdded(Id, CredentialType.WebAuthn));
        return Result.Success();
    }

    /// <summary>Enables multi-factor authentication.</summary>
    /// <param name="secret">The TOTP secret.</param>
    /// <param name="recoveryCodes">The recovery code hashes.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result EnableMfa(TotpSecret secret, IEnumerable<RecoveryCodeHash> recoveryCodes, DateTimeOffset nowUtc)
    {
        if (IsMfaEnabled)
        {
            return Result.Failure(IdentityErrors.MfaAlreadyEnabled);
        }

        var totp = new TotpCredential(CredentialId.New(), secret, nowUtc);
        totp.MarkVerified(nowUtc);
        _credentials.Add(totp);
        _credentials.Add(new RecoveryCodeCredential(CredentialId.New(), recoveryCodes, nowUtc));
        RaiseDomainEvent(new UserMfaEnabled(Id));
        return Result.Success();
    }

    /// <summary>Disables multi-factor authentication.</summary>
    public Result DisableMfa()
    {
        if (!IsMfaEnabled)
        {
            return Result.Failure(IdentityErrors.MfaNotEnabled);
        }

        _credentials.RemoveAll(credential => credential.Type is CredentialType.Totp or CredentialType.RecoveryCodes);
        RaiseDomainEvent(new UserMfaDisabled(Id));
        return Result.Success();
    }

    /// <summary>Consumes a recovery code.</summary>
    /// <param name="code">The hashed recovery code.</param>
    public Result ConsumeRecoveryCode(RecoveryCodeHash code)
    {
        RecoveryCodeCredential? credential = _credentials.OfType<RecoveryCodeCredential>().FirstOrDefault();

        if (credential is null)
        {
            return Result.Failure(IdentityErrors.MfaNotEnabled);
        }

        return credential.Consume(code);
    }

    /// <summary>Removes an owned credential.</summary>
    /// <param name="credentialId">The credential identifier.</param>
    public Result RemoveCredential(CredentialId credentialId)
    {
        Credential? credential = _credentials.Find(candidate => candidate.Id == credentialId);

        if (credential is null)
        {
            return Result.Failure(IdentityErrors.CredentialNotFound);
        }

        if (Status == UserStatus.Active && _credentials.Count == 1)
        {
            return Result.Failure(IdentityErrors.LastCredentialCannotBeRemoved);
        }

        _credentials.Remove(credential);
        RotateSecurityStamp();
        RaiseDomainEvent(new UserCredentialRemoved(Id, credential.Type));
        return Result.Success();
    }

    /// <summary>Requests a password reset, replacing any previous token (ADR-016).</summary>
    /// <param name="tokenId">The token identifier.</param>
    /// <param name="hash">The token hash.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="lifetime">The token lifetime.</param>
    public Result RequestPasswordReset(PasswordResetTokenId tokenId, PasswordResetTokenHash hash, DateTimeOffset nowUtc, TimeSpan lifetime)
    {
        PasswordResetToken = new PasswordResetToken(tokenId, hash, nowUtc, nowUtc.Add(lifetime));
        RaiseDomainEvent(new UserPasswordResetRequested(Id, tokenId, PasswordResetToken.ExpiresOnUtc));
        return Result.Success();
    }

    /// <summary>Completes a password reset using the provided token hash.</summary>
    /// <param name="providedHash">The presented token hash.</param>
    /// <param name="newHash">The new password hash.</param>
    /// <param name="nowUtc">The current UTC time.</param>
    public Result CompletePasswordReset(PasswordResetTokenHash providedHash, PasswordHash newHash, DateTimeOffset nowUtc)
    {
        if (PasswordResetToken is null)
        {
            return Result.Failure(IdentityErrors.PasswordResetTokenMissing);
        }

        Result validation = PasswordResetToken.Validate(providedHash, nowUtc);

        if (validation.IsFailure)
        {
            return validation;
        }

        PasswordResetToken.MarkUsed(nowUtc);
        RaiseDomainEvent(new UserPasswordResetCompleted(Id, PasswordResetToken.Id));
        return SetPassword(newHash, nowUtc);
    }

    /// <summary>Assigns a platform (system) role to the user.</summary>
    /// <param name="roleId">The role identifier.</param>
    public Result AssignPlatformRole(RoleId roleId)
    {
        if (!_platformRoleIds.Add(roleId))
        {
            return Result.Failure(IdentityErrors.MembershipRoleAlreadyAssigned);
        }

        RaiseDomainEvent(new UserPlatformRoleAssigned(Id, roleId));
        return Result.Success();
    }

    /// <summary>Removes a platform (system) role from the user.</summary>
    /// <param name="roleId">The role identifier.</param>
    public Result RemovePlatformRole(RoleId roleId)
    {
        if (!_platformRoleIds.Remove(roleId))
        {
            return Result.Failure(IdentityErrors.MembershipRoleNotAssigned);
        }

        RaiseDomainEvent(new UserPlatformRoleRemoved(Id, roleId));
        return Result.Success();
    }

    private void RotateSecurityStamp() => SecurityStamp = SecurityStamp.New();
}
