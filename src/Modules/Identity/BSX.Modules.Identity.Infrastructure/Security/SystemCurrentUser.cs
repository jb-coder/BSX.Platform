using BSX.BuildingBlocks.Authorization;

namespace BSX.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Default <see cref="ICurrentUser"/> used until the host registers an authenticated
/// implementation (ADR-012). Yields the reserved <c>system</c> actor.
/// </summary>
internal sealed class SystemCurrentUser : ICurrentUser
{
    /// <inheritdoc />
    public Guid? UserId => null;

    /// <inheritdoc />
    public string? Email => null;

    /// <inheritdoc />
    public bool IsAuthenticated => false;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => [];
}
