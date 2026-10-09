using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a user is locked until a given time.</summary>
public sealed record UserLocked(UserId UserId, DateTimeOffset UntilUtc) : DomainEvent;
