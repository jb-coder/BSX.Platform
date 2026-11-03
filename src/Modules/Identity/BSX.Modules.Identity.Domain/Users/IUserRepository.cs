using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Domain.Users;

/// <summary>Persistence contract for the <see cref="User"/> aggregate (aggregate-specific, Rule 6).</summary>
public interface IUserRepository
{
    /// <summary>Gets a user by identifier.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);

    /// <summary>Gets a user by email.</summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>Finds a user by the hash of their active password reset token.</summary>
    /// <param name="hash">The password reset token hash.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<User?> FindByPasswordResetTokenAsync(PasswordResetTokenHash hash, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a user with the given email exists (global uniqueness, ADR-013).</summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether another active user holds the given platform role. Used to protect the
    /// last active administrator from losing the role.
    /// </summary>
    /// <param name="roleId">The platform role identifier.</param>
    /// <param name="excludedUserId">The user excluded from the check.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> ExistsOtherActiveUserWithPlatformRoleAsync(
        RoleId roleId,
        UserId excludedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks added platform-role rows as inserts before saving so owned value-object collection
    /// changes are persisted for an existing user.
    /// </summary>
    /// <param name="user">The user whose platform-role collection changed.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task TrackPlatformRoleChangesAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>Adds a new user.</summary>
    /// <param name="user">The user.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
