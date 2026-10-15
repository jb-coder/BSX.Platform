namespace BSX.Modules.Identity.Application.Dtos;

/// <summary>A session projection.</summary>
public sealed record SessionDto(
    Guid Id,
    Guid? TenantId,
    string Status,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset LastRefreshedOnUtc,
    DateTimeOffset ExpiresOnUtc,
    string? UserAgent,
    string? IpAddress);
