using BSX.BuildingBlocks.Authorization;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;

internal sealed class TestTenantContext : ITenantContext
{
    public TestTenantContext(Guid tenantId) => TenantId = tenantId;

    public Guid? TenantId { get; }
}
