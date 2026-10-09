namespace BSX.Modules.Identity.Application.ServiceAccounts.CreateServiceAccount;

/// <summary>The result of creating a service account.</summary>
/// <param name="ServiceAccountId">The new service account identifier.</param>
public sealed record CreateServiceAccountResponse(Guid ServiceAccountId);
