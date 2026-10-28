namespace BSX.Modules.Identity.Application.Dtos;

/// <summary>A tenant membership option presented during sign-in.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="JoinedOnUtc">When the membership became active.</param>
public sealed record TenantOptionDto(Guid TenantId, DateTimeOffset? JoinedOnUtc);
