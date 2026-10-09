namespace BSX.Modules.Identity.Application.Dtos;

/// <summary>Role projection.</summary>
public sealed record RoleDto(
    Guid Id,
    Guid? TenantId,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions);
