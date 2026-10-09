using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user is invited to a tenant.</summary>
public sealed record MembershipInvited(MembershipId MembershipId, TenantId TenantId, UserId UserId) : DomainEvent;
