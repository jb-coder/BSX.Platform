using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.ApiKeys.CreateApiKey;

/// <summary>Creates an API key for a service account.</summary>
/// <param name="ServiceAccountId">The owning service account.</param>
/// <param name="Name">The operator label.</param>
/// <param name="ExpiresOnUtc">The optional expiry.</param>
/// <param name="Scopes">The optional permission codes granted to the key.</param>
public sealed record CreateApiKeyCommand(
    Guid ServiceAccountId,
    string Name,
    DateTimeOffset? ExpiresOnUtc,
    IReadOnlyCollection<string>? Scopes) : ICommand<CreateApiKeyResponse>;
