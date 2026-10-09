using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a platform (system) role is removed from a user.</summary>
public sealed record UserPlatformRoleRemoved(UserId UserId, RoleId RoleId) : DomainEvent;
