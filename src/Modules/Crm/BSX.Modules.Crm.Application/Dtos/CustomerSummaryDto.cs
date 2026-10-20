namespace BSX.Modules.Crm.Application.Dtos;

/// <summary>A lightweight customer projection for list screens.</summary>
public sealed record CustomerSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    Guid? OrganizationId);
