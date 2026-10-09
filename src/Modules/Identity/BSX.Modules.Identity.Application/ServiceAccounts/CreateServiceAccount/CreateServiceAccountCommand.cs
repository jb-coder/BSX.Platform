using BSX.BuildingBlocks.Cqrs;

namespace BSX.Modules.Identity.Application.ServiceAccounts.CreateServiceAccount;

/// <summary>Creates a service account.</summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="Name">The service account name.</param>
public sealed record CreateServiceAccountCommand(Guid TenantId, string Name) : ICommand<CreateServiceAccountResponse>;
