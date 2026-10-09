using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a role is removed from a service account.</summary>
public sealed record ServiceAccountRoleRemoved(ServiceAccountId ServiceAccountId, RoleId RoleId) : DomainEvent;
