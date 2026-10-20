namespace BSX.Modules.Crm.Application.Dtos;

/// <summary>An organization projection.</summary>
public sealed record OrganizationDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? TaxIdentifier,
    AddressDto? Address);
