namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Generates cryptographically secure opaque tokens. Implemented in Infrastructure.</summary>
public interface ISecureTokenGenerator
{
    /// <summary>Generates a base64url token of the given entropy.</summary>
    /// <param name="byteLength">The number of random bytes.</param>
    string GenerateToken(int byteLength = 32);
}
