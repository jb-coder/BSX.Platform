namespace BSX.Modules.Sales.Application.Dtos;

/// <summary>A lightweight opportunity projection for list screens.</summary>
public sealed record OpportunitySummaryDto(
    Guid Id,
    Guid LeadId,
    string Title,
    Guid CustomerId,
    Guid? OrganizationId,
    decimal Amount,
    string Currency,
    int Probability,
    DateOnly ExpectedCloseDate,
    string Stage,
    decimal WeightedAmount);
