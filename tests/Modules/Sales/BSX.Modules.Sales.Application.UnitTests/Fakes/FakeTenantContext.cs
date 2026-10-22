using BSX.BuildingBlocks.Authorization;

namespace BSX.Modules.Sales.Application.UnitTests.Fakes;

internal sealed class FakeTenantContext : ITenantContext
{
    public FakeTenantContext(Guid? tenantId) => TenantId = tenantId;

    public Guid? TenantId { get; }
}
