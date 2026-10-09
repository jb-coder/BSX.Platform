using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when multi-factor authentication is disabled for a user.</summary>
public sealed record UserMfaDisabled(UserId UserId) : DomainEvent;
