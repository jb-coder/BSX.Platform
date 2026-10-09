using BSX.Modules.Identity.Domain.Enums;
using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a credential is added to a user.</summary>
public sealed record UserCredentialAdded(UserId UserId, CredentialType CredentialType) : DomainEvent;
