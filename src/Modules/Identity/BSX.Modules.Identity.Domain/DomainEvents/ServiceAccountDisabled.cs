using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a service account is disabled.</summary>
public sealed record ServiceAccountDisabled(ServiceAccountId ServiceAccountId) : DomainEvent;
