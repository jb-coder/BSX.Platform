using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Inventory.Domain.DomainEvents;

/// <summary>Raised when a product is deactivated.</summary>
/// <param name="ProductId">The product identifier.</param>
public sealed record ProductDeactivated(ProductId ProductId) : DomainEvent;
