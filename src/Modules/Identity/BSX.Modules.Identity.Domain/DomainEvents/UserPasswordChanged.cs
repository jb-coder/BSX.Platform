using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user's password changes.</summary>
public sealed record UserPasswordChanged(UserId UserId) : DomainEvent;
