namespace BSX.Modules.Sales.Application.Dtos;

/// <summary>A lead detail projection.</summary>
public sealed record LeadDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string Status,
    Guid? CustomerId,
    Guid? OrganizationId,
    string? Notes);
