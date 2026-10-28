namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>Describes the access token to issue (ADR-017).</summary>
public sealed record AccessTokenDescriptor(
    Guid UserId,
    Guid? TenantId,
    Guid PrincipalId,
    string PrincipalType,
    string Email,
    string Name,
    IReadOnlyCollection<string> Permissions,
    string SecurityStamp,
    Guid SessionId);

/// <summary>An issued access token and its expiry.</summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAtUtc);
