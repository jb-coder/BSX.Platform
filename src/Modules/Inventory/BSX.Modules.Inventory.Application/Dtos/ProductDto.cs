namespace BSX.Modules.Inventory.Application.Dtos;

/// <summary>A product detail projection.</summary>
public sealed record ProductDto(
    Guid Id,
    Guid TenantId,
    string Sku,
    string Name,
    string Status);
