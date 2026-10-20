using BSX.BuildingBlocks.Authorization;

namespace BSX.Composition.IntegrationTests.TestDoubles;

internal sealed class TestTenantContext : ITenantContext
{
    public TestTenantContext(Guid tenantId) => TenantId = tenantId;

    public Guid? TenantId { get; }
}
