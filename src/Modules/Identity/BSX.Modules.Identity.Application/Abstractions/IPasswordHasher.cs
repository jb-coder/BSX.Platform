using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Hashes and verifies passwords. Implemented in Infrastructure.</summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password.</summary>
    /// <param name="plainTextPassword">The plaintext password.</param>
    PasswordHash Hash(string plainTextPassword);

    /// <summary>Verifies a plaintext password against a stored hash using a constant-time comparison.</summary>
    /// <param name="plainTextPassword">The plaintext password.</param>
    /// <param name="passwordHash">The stored password hash.</param>
    bool Verify(string plainTextPassword, PasswordHash passwordHash);
}
