using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;

namespace BSX.Modules.Sales.Application.Opportunities.CreateOpportunityFromLead;

/// <summary>Creates one opportunity from a qualified lead.</summary>
/// <param name="LeadId">The qualified source lead identifier.</param>
/// <param name="CustomerId">The required CRM customer reference.</param>
/// <param name="Title">The opportunity title.</param>
/// <param name="Amount">The potential revenue amount.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="Probability">The probability of winning, from 0 to 100.</param>
/// <param name="ExpectedCloseDate">The expected close date.</param>
[RequirePermission(SalesPermissions.OpportunitiesCreate)]
public sealed record CreateOpportunityFromLeadCommand(
    Guid LeadId,
    Guid CustomerId,
    string Title,
    decimal Amount,
    string Currency,
    int Probability,
    DateOnly ExpectedCloseDate) : ICommand<Guid>;
