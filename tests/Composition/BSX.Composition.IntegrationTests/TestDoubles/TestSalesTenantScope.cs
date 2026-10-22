using BSX.Modules.Sales.Infrastructure.Persistence;

namespace BSX.Composition.IntegrationTests.TestDoubles;

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
