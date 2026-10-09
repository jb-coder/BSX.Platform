using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a service account is enabled.</summary>
public sealed record ServiceAccountEnabled(ServiceAccountId ServiceAccountId) : DomainEvent;
