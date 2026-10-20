using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.Organizations.UpdateOrganization;

/// <summary>Updates an organization.</summary>
/// <param name="OrganizationId">The organization identifier.</param>
/// <param name="Name">The new name.</param>
/// <param name="TaxIdentifier">The new tax identifier, or <see langword="null"/> to clear it.</param>
/// <param name="Address">The new primary address, or <see langword="null"/> to clear it.</param>
[RequirePermission("crm.organizations.update")]
public sealed record UpdateOrganizationCommand(
    Guid OrganizationId,
    string Name,
    string? TaxIdentifier,
    AddressInput? Address) : ICommand;
