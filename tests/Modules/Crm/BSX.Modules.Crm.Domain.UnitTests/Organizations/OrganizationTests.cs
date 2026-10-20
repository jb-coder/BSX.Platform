using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.Organizations;

public sealed class OrganizationTests
{
    [Fact]
    public void Should_CreateOrganization_WhenValid()
    {
        OrganizationId id = OrganizationId.New();
        CustomerName name = TestData.Name("Holding Group");
        TaxIdentifier tax = TestData.Tax();
        Address address = TestData.Address();

        var result = Organization.Create(id, TestData.Tenant, name, tax, address);

        result.IsSuccess.Should().BeTrue();
        Organization organization = result.Value;
        organization.Id.Should().Be(id);
        organization.TenantId.Should().Be(TestData.Tenant);
        organization.Name.Should().Be(name);
        organization.TaxIdentifier.Should().Be(tax);
        organization.Address.Should().Be(address);
    }

    [Fact]
    public void Should_RenameOrganization_WhenRequested()
    {
        Organization organization = TestData.CreateOrganization();
        CustomerName name = TestData.Name("New Group Name");

        var result = organization.Rename(name);

        result.IsSuccess.Should().BeTrue();
        organization.Name.Should().Be(name);
    }

    [Fact]
    public void Should_ChangeTaxIdentifier_WhenRequested()
    {
        Organization organization = TestData.CreateOrganization();
        TaxIdentifier tax = TestData.Tax();

        organization.ChangeTaxIdentifier(tax).IsSuccess.Should().BeTrue();
        organization.TaxIdentifier.Should().Be(tax);

        organization.ChangeTaxIdentifier(null).IsSuccess.Should().BeTrue();
        organization.TaxIdentifier.Should().BeNull();
    }

    [Fact]
    public void Should_ChangeAddress_WhenRequested()
    {
        Organization organization = TestData.CreateOrganization();
        Address address = TestData.Address("5 Corporate Way");

        organization.ChangeAddress(address).IsSuccess.Should().BeTrue();
        organization.Address.Should().Be(address);

        organization.ChangeAddress(null).IsSuccess.Should().BeTrue();
        organization.Address.Should().BeNull();
    }
}
