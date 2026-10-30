namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// Default revalidator for hosts that do not install an identity store. It performs no store lookup
/// and leaves the existing permission claims authoritative.
/// </summary>
internal sealed class NullAuthorizationRevalidator : IAuthorizationRevalidator
{
    /// <inheritdoc />
    public Task<AuthorizationRevalidationResult> RevalidateAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(AuthorizationRevalidationResult.Allowed());
}
