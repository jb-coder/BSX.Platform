using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a platform (system) role is assigned to a user.</summary>
public sealed record UserPlatformRoleAssigned(UserId UserId, RoleId RoleId) : DomainEvent;
