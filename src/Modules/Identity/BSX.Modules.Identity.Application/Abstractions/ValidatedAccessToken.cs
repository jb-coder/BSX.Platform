namespace BSX.Modules.Identity.Application.Abstractions;

/// <summary>The principal extracted from a validated access token.</summary>
public sealed record ValidatedAccessToken(
    Guid UserId,
    Guid? TenantId,
    Guid PrincipalId,
    string PrincipalType,
    IReadOnlyCollection<string> Permissions,
    string SecurityStamp,
    Guid SessionId,
    DateTimeOffset ExpiresAtUtc);
