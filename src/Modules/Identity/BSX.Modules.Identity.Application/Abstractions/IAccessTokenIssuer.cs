namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Issues stateless JWT access tokens (ADR-017). Implemented in Infrastructure.</summary>
public interface IAccessTokenIssuer
{
    /// <summary>Issues an access token for the described principal.</summary>
    /// <param name="descriptor">The token descriptor.</param>
    AccessToken Issue(AccessTokenDescriptor descriptor);
}
