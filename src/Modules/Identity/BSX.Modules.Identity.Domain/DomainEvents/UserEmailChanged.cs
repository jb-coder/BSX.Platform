using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user's email address changes.</summary>
public sealed record UserEmailChanged(UserId UserId, string Email) : DomainEvent;
