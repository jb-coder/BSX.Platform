namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// Revalidates the current principal against the authoritative identity store before a request is
/// authorized (ADR-021). Hosts without an identity store use the non-authoritative default.
/// </summary>
public interface IAuthorizationRevalidator
{
    /// <summary>
    /// Revalidates the current principal. Implementations must fail closed: any uncertainty must
    /// return <see cref="AuthorizationRevalidationResult.Denied"/>.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<AuthorizationRevalidationResult> RevalidateAsync(CancellationToken cancellationToken = default);
}
