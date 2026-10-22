namespace BSX.Modules.Sales.Application.Dtos;

/// <summary>An opportunity detail projection, including the weighted revenue forecast.</summary>
public sealed record OpportunityDto(
    Guid Id,
    Guid TenantId,
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
