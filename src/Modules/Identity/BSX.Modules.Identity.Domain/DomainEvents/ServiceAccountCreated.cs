using BSX.Modules.Identity.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Identity.Domain.DomainEvents;

/// <summary>Raised when a service account is created.</summary>
public sealed record ServiceAccountCreated(ServiceAccountId ServiceAccountId, TenantId TenantId, string Name) : DomainEvent;
