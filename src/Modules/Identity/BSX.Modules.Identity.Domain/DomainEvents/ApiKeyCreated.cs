using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when an API key is created.</summary>
public sealed record ApiKeyCreated(ApiKeyId ApiKeyId, PrincipalId PrincipalId, string Name) : DomainEvent;
