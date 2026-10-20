namespace BSX.Modules.Crm.Application.Dtos;

/// <summary>A customer detail projection with owned addresses and contact persons.</summary>
public sealed record CustomerDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    string? TaxIdentifier,
    Guid? OrganizationId,
    string Status,
    IReadOnlyList<AddressDto> Addresses,
    IReadOnlyList<ContactDto> Contacts);
