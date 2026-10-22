using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;

namespace BSX.Modules.Sales.Application.Opportunities.UpdateOpportunity;

/// <summary>Updates an open opportunity's details and revenue forecast.</summary>
/// <param name="OpportunityId">The opportunity identifier.</param>
/// <param name="CustomerId">The required CRM customer reference.</param>
/// <param name="OrganizationId">The optional CRM organization reference.</param>
/// <param name="Title">The opportunity title.</param>
/// <param name="Amount">The potential revenue amount.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="Probability">The probability of winning, from 0 to 100.</param>
/// <param name="ExpectedCloseDate">The expected close date.</param>
[RequirePermission(SalesPermissions.OpportunitiesUpdate)]
public sealed record UpdateOpportunityCommand(
    Guid OpportunityId,
    Guid CustomerId,
    Guid? OrganizationId,
    string Title,
    decimal Amount,
    string Currency,
    int Probability,
    DateOnly ExpectedCloseDate) : ICommand;
