using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Sales.Application.Common;

namespace BSX.Modules.Sales.Application.Leads.UpdateLead;

/// <summary>
/// Updates a lead. Details are editable only while the lead is new or contacted; notes remain
/// editable in every status.
/// </summary>
/// <param name="LeadId">The lead identifier.</param>
/// <param name="Title">The lead title.</param>
/// <param name="CustomerId">The optional CRM customer reference.</param>
/// <param name="OrganizationId">The optional CRM organization reference.</param>
/// <param name="Notes">The optional free-text notes.</param>
[RequirePermission(SalesPermissions.LeadsUpdate)]
public sealed record UpdateLeadCommand(
    Guid LeadId,
    string Title,
    Guid? CustomerId,
    Guid? OrganizationId,
    string? Notes) : ICommand;
