using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a suspended membership is reactivated.</summary>
public sealed record MembershipReactivated(MembershipId MembershipId, TenantId TenantId, UserId UserId) : DomainEvent;
