using BSX.BuildingBlocks.Authorization;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;

internal sealed class TestCurrentUser : ICurrentUser
{
    public TestCurrentUser(Guid? userId = null, Guid? tenantId = null)
    {
        UserId = userId;
        TenantId = tenantId;
    }

    public Guid? UserId { get; }

    public Guid? TenantId { get; }

    public string? Email => null;

    public bool IsAuthenticated => UserId.HasValue;

    public IReadOnlyCollection<string> Permissions => [];
}
