using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Common;

namespace BSX.Modules.Identity.Application.ApiKeys.RevokeApiKey;

/// <summary>Revokes an API key.</summary>
/// <param name="ApiKeyId">The API key identifier.</param>
[RequirePermission(IdentityPermissionCodes.ApiKeysRevoke)]
public sealed record RevokeApiKeyCommand(Guid ApiKeyId) : ICommand;
