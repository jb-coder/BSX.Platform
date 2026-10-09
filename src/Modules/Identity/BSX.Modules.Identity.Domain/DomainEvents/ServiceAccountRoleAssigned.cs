using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a role is assigned to a service account.</summary>
public sealed record ServiceAccountRoleAssigned(ServiceAccountId ServiceAccountId, RoleId RoleId) : DomainEvent;
