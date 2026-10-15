using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a session starts (login).</summary>
public sealed record SessionStarted(Sessions.SessionId SessionId, ValueObjects.UserId UserId, Guid? TenantId) : DomainEvent;
