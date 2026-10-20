using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.Organizations.CreateOrganization;

/// <summary>Creates an organization in the active tenant.</summary>
/// <param name="Name">The organization name.</param>
/// <param name="TaxIdentifier">The optional tax identifier.</param>
/// <param name="Address">The optional primary address.</param>
[RequirePermission("crm.organizations.create")]
public sealed record CreateOrganizationCommand(
    string Name,
    string? TaxIdentifier,
    AddressInput? Address) : ICommand<Guid>;
