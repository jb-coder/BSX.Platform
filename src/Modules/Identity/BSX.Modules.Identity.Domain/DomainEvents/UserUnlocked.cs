using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user is unlocked.</summary>
public sealed record UserUnlocked(UserId UserId) : DomainEvent;
