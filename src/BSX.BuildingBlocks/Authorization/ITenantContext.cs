namespace BSX.BuildingBlocks.Authorization;

/// <summary>Resolves the tenant the current request is scoped to (ADR-013).</summary>
public interface ITenantContext
{
    /// <summary>Gets the active tenant identifier, if any.</summary>
    Guid? TenantId { get; }
}
