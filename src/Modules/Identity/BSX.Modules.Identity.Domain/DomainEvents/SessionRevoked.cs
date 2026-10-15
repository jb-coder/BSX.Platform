using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a session is revoked.</summary>
public sealed record SessionRevoked(Sessions.SessionId SessionId, ValueObjects.UserId UserId, string Reason) : DomainEvent;
