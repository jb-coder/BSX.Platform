using BSX.BuildingBlocks.Authorization;

namespace BSX.BuildingBlocks.UnitTests.Fixtures;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(
        Guid? userId = null,
        Guid? tenantId = null,
        bool isAuthenticated = false,
        params string[] permissions)
    {
        UserId = userId;
        TenantId = tenantId;
        IsAuthenticated = isAuthenticated;
        Permissions = permissions;
    }

    public Guid? UserId { get; }

    public Guid? TenantId { get; }

    public string? Email => null;

    public bool IsAuthenticated { get; }

    public IReadOnlyCollection<string> Permissions { get; }
}
