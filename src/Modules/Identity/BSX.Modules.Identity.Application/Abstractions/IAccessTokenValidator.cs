namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Validates stateless JWT access tokens (ADR-017). Implemented in Infrastructure.</summary>
public interface IAccessTokenValidator
{
    /// <summary>Validates a token and extracts the principal.</summary>
    /// <param name="token">The access token.</param>
    /// <param name="principal">The extracted principal on success.</param>
    bool TryValidate(string token, out ValidatedAccessToken principal);
}
