using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user is activated.</summary>
public sealed record UserActivated(UserId UserId) : DomainEvent;
