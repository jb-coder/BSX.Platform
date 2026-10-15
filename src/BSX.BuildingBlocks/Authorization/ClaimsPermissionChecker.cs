namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// Default fail-closed permission checker backed by the current principal's permissions claim.
/// </summary>
internal sealed class ClaimsPermissionChecker : IPermissionChecker
{
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="ClaimsPermissionChecker"/> class.</summary>
    /// <param name="currentUser">The current user.</param>
    public ClaimsPermissionChecker(ICurrentUser currentUser) => _currentUser = currentUser;

    /// <inheritdoc />
    public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(permission))
        {
            return Task.FromResult(false);
        }

        bool granted = _currentUser.Permissions.Contains(permission, StringComparer.Ordinal);
        return Task.FromResult(granted);
    }
}
