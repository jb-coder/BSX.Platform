namespace BSX.Modules.Identity.Application.Dtos;

/// <summary>Lightweight user projection for lists.</summary>
public sealed record UserSummaryDto(Guid Id, string Email, string Name, string Status);
