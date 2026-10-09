using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a tenant role is assigned to a membership.</summary>
public sealed record MembershipRoleAssigned(MembershipId MembershipId, TenantId TenantId, UserId UserId, RoleId RoleId) : DomainEvent;
