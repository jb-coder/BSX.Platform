using BSX.Modules.Crm.Infrastructure.Persistence;

namespace BSX.Composition.IntegrationTests.TestDoubles;

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
