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

    /// <summary>Determines whether a user with the given email exists (global uniqueness, ADR-013).</summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>Adds a new user.</summary>
    /// <param name="user">The user.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
