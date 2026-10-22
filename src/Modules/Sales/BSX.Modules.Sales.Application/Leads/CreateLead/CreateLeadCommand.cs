using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;

namespace BSX.Modules.Sales.Application.Leads.CreateLead;

/// <summary>Creates a lead in the active tenant.</summary>
/// <param name="Title">The lead title.</param>
/// <param name="CustomerId">The optional CRM customer reference.</param>
/// <param name="OrganizationId">The optional CRM organization reference.</param>
/// <param name="Notes">The optional free-text notes.</param>
[RequirePermission(SalesPermissions.LeadsCreate)]
public sealed record CreateLeadCommand(
    string Title,
    Guid? CustomerId,
    Guid? OrganizationId,
    string? Notes) : ICommand<Guid>;
