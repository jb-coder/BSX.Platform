namespace BSX.BuildingBlocks.Authorization;

/// <summary>
/// Default <see cref="ITenantContext"/> that resolves the active tenant from the current principal
/// (ADR-013). The host is responsible for populating <see cref="ICurrentUser.TenantId"/>.
/// </summary>
internal sealed class CurrentUserTenantContext : ITenantContext
{
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="CurrentUserTenantContext"/> class.</summary>
    /// <param name="currentUser">The current user.</param>
    public CurrentUserTenantContext(ICurrentUser currentUser) => _currentUser = currentUser;

    /// <inheritdoc />
    public Guid? TenantId => _currentUser.TenantId;
}
