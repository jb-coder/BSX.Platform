using System.Security.Cryptography;
using System.Text;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>SHA-256 hasher for password reset tokens (ADR-016).</summary>
internal sealed class PasswordResetTokenHasher : IPasswordResetTokenHasher
{
    /// <inheritdoc />
    public PasswordResetTokenHash Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return PasswordResetTokenHash.From(Convert.ToBase64String(digest));
    }
}
