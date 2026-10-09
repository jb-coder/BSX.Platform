using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a password reset completes.</summary>
public sealed record UserPasswordResetCompleted(UserId UserId, PasswordResetTokenId TokenId) : DomainEvent;
