using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when refresh-token reuse (theft) is detected.</summary>
public sealed record RefreshTokenReuseDetected(Sessions.SessionId SessionId, ValueObjects.UserId UserId, Sessions.RefreshTokenId TokenId) : DomainEvent;
