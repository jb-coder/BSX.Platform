using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Dtos;

namespace BSX.Modules.Identity.Application.Authentication.DiscoverTenantOptions;

/// <summary>Discovers the tenants a user may sign in to after credential validation.</summary>
/// <param name="Email">The account email.</param>
/// <param name="Password">The account password.</param>
[AllowAnonymous]
public sealed record DiscoverTenantOptionsCommand(
    string Email,
    string Password) : ICommand<IReadOnlyList<TenantOptionDto>>;
