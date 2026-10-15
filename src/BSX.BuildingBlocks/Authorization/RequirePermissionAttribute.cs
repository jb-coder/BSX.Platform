namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// Declares the permission required to execute a command or query (ADR-012).
/// Enforced by the authorization pipeline behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.</summary>
    /// <param name="permission">The required permission code.</param>
    public RequirePermissionAttribute(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
    }

    /// <summary>Gets the required permission code.</summary>
    public string Permission { get; }
}
