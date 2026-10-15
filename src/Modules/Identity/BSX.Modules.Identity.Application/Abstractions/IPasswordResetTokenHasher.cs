using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Hashes password reset tokens for storage (ADR-016). Only hashes are persisted.</summary>
public interface IPasswordResetTokenHasher
{
    /// <summary>Hashes a plaintext reset token.</summary>
    /// <param name="token">The plaintext token.</param>
    PasswordResetTokenHash Hash(string token);
}
