using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a role is renamed.</summary>
public sealed record RoleRenamed(RoleId RoleId, string Name) : DomainEvent;
