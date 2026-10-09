using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user is registered.</summary>
public sealed record UserRegistered(UserId UserId, string Email) : DomainEvent;
