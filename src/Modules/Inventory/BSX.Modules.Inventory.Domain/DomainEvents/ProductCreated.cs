using BSX.Modules.Inventory.Domain.ValueObjects;
using BSX.SharedKernel.Primitives;

namespace BSX.Modules.Inventory.Domain.DomainEvents;

/// <summary>Raised when a product is created.</summary>
/// <param name="ProductId">The product identifier.</param>
/// <param name="Sku">The product SKU.</param>
public sealed record ProductCreated(ProductId ProductId, Sku Sku) : DomainEvent;
