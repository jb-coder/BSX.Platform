using BSX.Modules.Identity.Domain.ValueObjects;

namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Hashes API key secrets. Implemented in Infrastructure.</summary>
public interface IApiKeyHasher
{
    /// <summary>Hashes an API key secret.</summary>
    /// <param name="secret">The plaintext secret.</param>
    ApiKeyHash Hash(string secret);
}
