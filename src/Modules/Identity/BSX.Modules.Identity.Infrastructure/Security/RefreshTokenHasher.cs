using System.Security.Cryptography;
using System.Text;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>SHA-256 hasher for opaque refresh tokens (ADR-017).</summary>
internal sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    /// <inheritdoc />
    public RefreshTokenHash Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return RefreshTokenHash.From(Convert.ToBase64String(digest));
    }
}
