namespace BSX.Modules.Crm.Application.Dtos;

/// <summary>A customer contact person projection.</summary>
public sealed record ContactDto(
    Guid Id,
    string Name,
    string? Email,
    string? PhoneNumber,
    bool IsPrimary);
