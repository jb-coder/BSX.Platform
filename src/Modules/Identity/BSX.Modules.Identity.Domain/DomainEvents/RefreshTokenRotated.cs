using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a refresh token is rotated.</summary>
public sealed record RefreshTokenRotated(Sessions.SessionId SessionId, ValueObjects.UserId UserId, Sessions.RefreshTokenId NewTokenId) : DomainEvent;
