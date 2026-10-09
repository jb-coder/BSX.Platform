namespace BSX.Modules.Identity.Application.Dtos;

/// <summary>Full user projection.</summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string Name,
    string Status,
    bool MfaEnabled,
    IReadOnlyList<Guid> PlatformRoleIds,
    DateTimeOffset RegisteredOnUtc);
