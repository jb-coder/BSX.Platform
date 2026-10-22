namespace BSX.Contracts.Permissions;

/// <summary>
/// Declares a permission owned by a module and published through the permission catalog (ADR-012).
/// Permission codes are stable, namespaced public contracts.
/// </summary>
/// <param name="Code">The stable permission code in the form <c>module.resource.action</c>.</param>
/// <param name="Description">A human-readable description of the capability.</param>
public sealed record PermissionDefinition(string Code, string Description);
