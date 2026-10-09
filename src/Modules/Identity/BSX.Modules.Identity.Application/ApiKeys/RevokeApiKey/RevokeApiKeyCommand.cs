using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.ApiKeys.RevokeApiKey;

/// <summary>Revokes an API key.</summary>
/// <param name="ApiKeyId">The API key identifier.</param>
public sealed record RevokeApiKeyCommand(Guid ApiKeyId) : ICommand;
