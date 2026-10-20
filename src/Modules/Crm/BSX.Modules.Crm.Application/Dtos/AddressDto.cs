namespace BSX.Modules.Crm.Application.Dtos;

/// <summary>A customer address projection.</summary>
public sealed record AddressDto(
    Guid Id,
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string Country,
    string Type,
    bool IsDefault);
