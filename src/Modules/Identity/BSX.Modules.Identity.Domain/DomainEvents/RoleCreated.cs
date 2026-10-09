using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a role is created.</summary>
public sealed record RoleCreated(RoleId RoleId, TenantId? TenantId, string Name) : DomainEvent;
