using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Hashes and verifies passwords. Implemented in Infrastructure.</summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password.</summary>
    /// <param name="plainTextPassword">The plaintext password.</param>
    PasswordHash Hash(string plainTextPassword);
}
