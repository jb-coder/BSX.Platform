using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a membership becomes active.</summary>
public sealed record MembershipActivated(MembershipId MembershipId, TenantId TenantId, UserId UserId) : DomainEvent;
