using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a password reset is requested.</summary>
public sealed record UserPasswordResetRequested(UserId UserId, PasswordResetTokenId TokenId, DateTimeOffset ExpiresOnUtc) : DomainEvent;
