using BSX.BuildingBlocks.Authorization;

namespace BSX.Modules.Identity.Application.UnitTests.Fakes;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(Guid? userId = null, Guid? tenantId = null, params string[] permissions)
    {
        UserId = userId;
        TenantId = tenantId;
        Permissions = permissions;
    }

    public Guid? UserId { get; }

    public Guid? TenantId { get; }

    public string? Email => null;

    public bool IsAuthenticated => UserId is not null;

    public IReadOnlyCollection<string> Permissions { get; }
}
