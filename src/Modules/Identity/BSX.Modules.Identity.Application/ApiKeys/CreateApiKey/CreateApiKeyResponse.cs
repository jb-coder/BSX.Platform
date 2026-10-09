namespace BSX.Modules.Identity.Application.ApiKeys.CreateApiKey;

/// <summary>The result of creating an API key. The secret is shown only once.</summary>
/// <param name="ApiKeyId">The new API key identifier.</param>
/// <param name="Secret">The plaintext secret, shown only once.</param>
/// <param name="Prefix">The non-secret display prefix.</param>
public sealed record CreateApiKeyResponse(Guid ApiKeyId, string Secret, string Prefix);
