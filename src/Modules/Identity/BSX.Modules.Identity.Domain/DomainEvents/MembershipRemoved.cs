using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user is removed from a tenant.</summary>
public sealed record MembershipRemoved(MembershipId MembershipId, TenantId TenantId, UserId UserId) : DomainEvent;
