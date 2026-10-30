namespace BSX.Contracts.Permissions;

/// <summary>
/// Defines the authorization boundary a permission protects and, therefore, which roles may grant it
/// (ADR-020). The scope is declared with the permission and is never inferred from the caller.
/// </summary>
public enum PermissionScope
{
    /// <summary>
    /// A capability over platform-global data (for example the global user directory). Only platform
    /// roles (roles without a tenant) may grant it. The default is the most restrictive scope so an
    /// unspecified value fails closed.
    /// </summary>
    Platform = 0,

    /// <summary>
    /// A capability confined to the caller's active tenant (for example CRM customers). Tenant roles
    /// and platform roles may grant it; tenant data access is still constrained by the tenant guard
    /// and tenant query filters.
    /// </summary>
    Tenant = 1,
}
