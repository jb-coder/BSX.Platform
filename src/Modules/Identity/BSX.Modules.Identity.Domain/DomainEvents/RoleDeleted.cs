using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a role is deleted.</summary>
public sealed record RoleDeleted(RoleId RoleId) : DomainEvent;
