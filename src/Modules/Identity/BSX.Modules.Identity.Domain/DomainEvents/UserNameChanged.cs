using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user's display name changes.</summary>
public sealed record UserNameChanged(UserId UserId, string Name) : DomainEvent;
