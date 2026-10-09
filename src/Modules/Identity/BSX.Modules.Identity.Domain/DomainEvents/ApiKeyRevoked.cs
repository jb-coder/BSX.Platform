using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when an API key is revoked.</summary>
public sealed record ApiKeyRevoked(ApiKeyId ApiKeyId, PrincipalId PrincipalId) : DomainEvent;
