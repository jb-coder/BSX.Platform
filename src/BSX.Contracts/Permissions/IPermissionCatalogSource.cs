namespace BSX.Contracts.Permissions;

/// <summary>
/// Exposes the permissions declared by a module so the Identity module can seed and synchronize
/// the platform permission catalog (ADR-018). Modules register one source per module; Identity
/// never references the module implementation.
/// </summary>
public interface IPermissionCatalogSource
{
    /// <summary>Gets the permissions declared by the owning module.</summary>
    IReadOnlyCollection<PermissionDefinition> Permissions { get; }
}
