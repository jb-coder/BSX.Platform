namespace BSX.Modules.Sales.Application.Dtos;

/// <summary>A lightweight lead projection for list screens.</summary>
public sealed record LeadSummaryDto(
    Guid Id,
    string Title,
    string Status,
    Guid? CustomerId,
    Guid? OrganizationId);
