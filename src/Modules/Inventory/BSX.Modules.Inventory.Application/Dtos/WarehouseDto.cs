namespace BSX.Modules.Inventory.Application.Dtos;

/// <summary>A warehouse detail projection.</summary>
public sealed record WarehouseDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Status);
