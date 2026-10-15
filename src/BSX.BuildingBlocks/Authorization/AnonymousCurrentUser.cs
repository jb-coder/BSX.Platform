namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// Default anonymous <see cref="ICurrentUser"/>. The host replaces it with an authenticated
/// implementation from the request principal (ADR-012).
/// </summary>
internal sealed class AnonymousCurrentUser : ICurrentUser
{
    /// <inheritdoc />
    public Guid? UserId => null;

    /// <inheritdoc />
    public Guid? TenantId => null;

    /// <inheritdoc />
    public string? Email => null;

    /// <inheritdoc />
    public bool IsAuthenticated => false;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => [];
}
