using BSX.Contracts.Permissions;

namespace BSX.Modules.Identity.Application.Dtos;

/// <summary>A permission-catalog entry.</summary>
/// <param name="Code">The stable permission code.</param>
/// <param name="Description">The human-readable description.</param>
/// <param name="Scope">
/// The authorization boundary the permission protects. Clients must use it to present only the
/// permissions that are valid for the role being edited (ADR-020).
/// </param>
public sealed record PermissionDto(string Code, string Description, PermissionScope Scope);
