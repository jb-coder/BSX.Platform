using System.Security.Cryptography;
using System.Text;
using BSX.Modules.Identity.Application.Abstractions;
using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>SHA-256 API key secret hasher (ADR-014).</summary>
internal sealed class ApiKeyHasher : IApiKeyHasher
{
    /// <inheritdoc />
    public ApiKeyHash Hash(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return ApiKeyHash.From(Convert.ToBase64String(digest));
    }
}
