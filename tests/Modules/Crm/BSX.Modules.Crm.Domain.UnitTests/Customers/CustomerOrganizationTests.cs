using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.DomainEvents;
using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.Customers;

public sealed class CustomerOrganizationTests
{
    [Fact]
    public void Should_RaiseOrganizationAssigned_WhenAssigned()
    {
        Customer customer = TestData.CreateCustomer();
        OrganizationId organizationId = OrganizationId.New();

        var result = customer.AssignOrganization(organizationId);

        result.IsSuccess.Should().BeTrue();
        customer.OrganizationId.Should().Be(organizationId);

        OrganizationAssigned assigned = customer.DomainEvents.OfType<OrganizationAssigned>().Single();
        assigned.CustomerId.Should().Be(customer.Id);
        assigned.OrganizationId.Should().Be(organizationId);
    }

    [Fact]
    public void Should_ReplaceOrganization_WhenDifferentOrganizationAssigned()
    {
        Customer customer = TestData.CreateCustomer();
        customer.AssignOrganization(OrganizationId.New());
        customer.ClearDomainEvents();
        OrganizationId replacement = OrganizationId.New();

        customer.AssignOrganization(replacement).IsSuccess.Should().BeTrue();

        customer.OrganizationId.Should().Be(replacement);
        customer.DomainEvents.OfType<OrganizationAssigned>().Single().OrganizationId.Should().Be(replacement);
    }

    [Fact]
    public void Should_Fail_WhenAssigningTheSameOrganization()
    {
        Customer customer = TestData.CreateCustomer();
        OrganizationId organizationId = OrganizationId.New();
        customer.AssignOrganization(organizationId);
        customer.ClearDomainEvents();

        var result = customer.AssignOrganization(organizationId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.OrganizationAlreadyAssigned);
        customer.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_WhenAssigningOrganizationToDeactivatedCustomer()
    {
        Customer customer = TestData.CreateCustomer();
        customer.Deactivate();

        var result = customer.AssignOrganization(OrganizationId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.CustomerNotActive);
    }

    [Fact]
    public void Should_RaiseOrganizationUnassigned_WhenCleared()
    {
        Customer customer = TestData.CreateCustomer();
        customer.AssignOrganization(OrganizationId.New());
        customer.ClearDomainEvents();

        var result = customer.ClearOrganization();

        result.IsSuccess.Should().BeTrue();
        customer.OrganizationId.Should().BeNull();
        customer.DomainEvents.OfType<OrganizationUnassigned>().Single().CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public void Should_Fail_WhenClearingOrganization_ButNotAssigned()
    {
        Customer customer = TestData.CreateCustomer();

        var result = customer.ClearOrganization();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.OrganizationNotAssigned);
        customer.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_AllowClearingOrganization_WhenCustomerDeactivated()
    {
        Customer customer = TestData.CreateCustomer();
        customer.AssignOrganization(OrganizationId.New());
        customer.Deactivate();

        customer.ClearOrganization().IsSuccess.Should().BeTrue();
        customer.OrganizationId.Should().BeNull();
    }
}
