using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a membership is suspended.</summary>
public sealed record MembershipSuspended(MembershipId MembershipId, TenantId TenantId, UserId UserId) : DomainEvent;
