using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a permission is granted to or revoked from a role.</summary>
public sealed record RolePermissionsChanged(RoleId RoleId, string PermissionCode, bool Granted) : DomainEvent;
