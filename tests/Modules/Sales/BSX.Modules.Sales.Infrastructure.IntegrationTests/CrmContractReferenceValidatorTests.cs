using BSX.Modules.Sales.Domain.ValueObjects;
using BSX.Modules.Sales.Infrastructure.Crm;
using BSX.Modules.Sales.Infrastructure.IntegrationTests.TestInfrastructure;

namespace BSX.Modules.Sales.Infrastructure.IntegrationTests;

/// <summary>
/// Verifies the CRM contract seam: Sales delegates to CRM.Contracts with the tenant and reference,
/// and the default lookup rejects everything when CRM is not composed.
/// </summary>
public sealed class CrmContractReferenceValidatorTests
{
    [Fact]
    public async Task Should_DelegateTenantAndReferences_ToTheCrmContract()
    {
        var lookup = new FakeCrmReferenceLookup { CustomerExists = true, OrganizationExists = true };
        var validator = new CrmContractReferenceValidator(lookup);
        Guid tenantId = Guid.NewGuid();
        Guid customerId = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();

        bool customer = await validator.CustomerExistsAsync(
            TenantId.From(tenantId),
            CustomerId.From(customerId));
        bool organization = await validator.OrganizationExistsAsync(
            TenantId.From(tenantId),
            OrganizationId.From(organizationId));

        customer.Should().BeTrue();
        organization.Should().BeTrue();
        lookup.CustomerCalls.Should().ContainSingle(call =>
            call.TenantId == tenantId && call.ReferenceId == customerId);
        lookup.OrganizationCalls.Should().ContainSingle(call =>
            call.TenantId == tenantId && call.ReferenceId == organizationId);
    }

    [Fact]
    public async Task FailClosedLookup_Should_RejectEveryReference()
    {
        var lookup = new FailClosedCrmReferenceLookup();

        (await lookup.CustomerExistsAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeFalse();
        (await lookup.OrganizationExistsAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeFalse();
    }
}
