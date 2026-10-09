using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user is deactivated.</summary>
public sealed record UserDeactivated(UserId UserId) : DomainEvent;
