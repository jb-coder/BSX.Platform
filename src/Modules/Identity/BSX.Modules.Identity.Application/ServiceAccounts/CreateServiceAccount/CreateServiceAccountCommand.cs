using BSX.BuildingBlocks.Authorization;
using BSX.BuildingBlocks.Cqrs;
using BSX.Modules.Identity.Application.Common;

namespace BSX.Modules.Identity.Application.ServiceAccounts.CreateServiceAccount;

/// <summary>Creates a service account.</summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="Name">The service account name.</param>
[RequirePermission(IdentityPermissionCodes.ServiceAccountsCreate)]
public sealed record CreateServiceAccountCommand(Guid TenantId, string Name) : ICommand<CreateServiceAccountResponse>;
