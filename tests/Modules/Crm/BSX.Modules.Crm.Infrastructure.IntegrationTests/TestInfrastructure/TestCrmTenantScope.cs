using BSX.Modules.Crm.Infrastructure.Persistence;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests.TestInfrastructure;

internal sealed class TestCrmTenantScope : ICrmTenantScope
{
    public TestCrmTenantScope(Guid? tenantId, bool isSystem = false)
    {
        TenantId = tenantId;
        IsSystem = isSystem;
    }

    public Guid? TenantId { get; }

    public bool IsSystem { get; }
}
