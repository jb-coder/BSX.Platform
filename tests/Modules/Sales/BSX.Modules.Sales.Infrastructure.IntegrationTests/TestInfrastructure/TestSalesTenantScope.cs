using BSX.Modules.Sales.Infrastructure.Persistence;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;

internal sealed class TestSalesTenantScope : ISalesTenantScope
{
    public TestSalesTenantScope(Guid? tenantId, bool isSystem = false)
    {
        TenantId = tenantId;
        IsSystem = isSystem;
    }

    public Guid? TenantId { get; }

    public bool IsSystem { get; }
}
