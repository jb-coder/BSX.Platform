namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// Evaluates whether the current principal holds a permission (ADR-012).
/// Implementations must be fail-closed: when in doubt, deny.
/// </summary>
public interface IPermissionChecker
{
    /// <summary>Determines whether the current principal holds the given permission.</summary>
    /// <param name="permission">The permission code (<c>module.resource.action</c>).</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default);
}
