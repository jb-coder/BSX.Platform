using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Hashes opaque refresh tokens for storage (ADR-017).</summary>
public interface IRefreshTokenHasher
{
    /// <summary>Hashes a plaintext refresh token.</summary>
    /// <param name="token">The plaintext token.</param>
    RefreshTokenHash Hash(string token);
}
