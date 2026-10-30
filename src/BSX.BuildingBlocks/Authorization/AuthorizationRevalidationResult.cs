namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// The outcome of revalidating the current principal against the authoritative identity store.
/// </summary>
/// <param name="IsValid">Whether the principal's session, account and membership are still valid.</param>
/// <param name="IsAuthoritative">
/// Whether <paramref name="Permissions"/> is the freshly resolved permission set. When
/// <see langword="false"/>, the consumer falls back to the registered
/// <see cref="IPermissionChecker"/> (for hosts without an identity store).
/// </param>
/// <param name="Permissions">The freshly resolved permissions when authoritative.</param>
public sealed record AuthorizationRevalidationResult(
    bool IsValid,
    bool IsAuthoritative,
    IReadOnlyCollection<string> Permissions)
{
    /// <summary>The principal is valid and its existing permission claims are authoritative.</summary>
    public static AuthorizationRevalidationResult Allowed()
        => new(true, false, []);

    /// <summary>The principal is valid and the supplied permission set is authoritative.</summary>
    /// <param name="permissions">The freshly resolved permissions.</param>
    public static AuthorizationRevalidationResult Authoritative(IReadOnlyCollection<string> permissions)
        => new(true, true, permissions);

    /// <summary>The principal is no longer valid and must be denied.</summary>
    public static AuthorizationRevalidationResult Denied { get; } = new(false, true, []);
}
