using System.Security.Cryptography;
using BSX.Modules.Identity.Application.Abstractions;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>Cryptographically secure base64url token generator (ADR-014/ADR-016).</summary>
internal sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    /// <inheritdoc />
    public string GenerateToken(int byteLength = 32)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(byteLength);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
