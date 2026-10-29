namespace BSX.Modules.Identity.Application.Dtos;

/// <summary>A workspace membership projection with its user and role context.</summary>
public sealed record MembershipDto(
    Guid Id,
    Guid UserId,
    string Email,
    string Name,
    string Status,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<string> RoleNames,
    DateTimeOffset InvitedOnUtc,
    DateTimeOffset? JoinedOnUtc);
