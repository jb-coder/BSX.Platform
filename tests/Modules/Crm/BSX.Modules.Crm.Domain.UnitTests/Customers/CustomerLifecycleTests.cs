using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.DomainEvents;
using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.Customers;

public sealed class CustomerLifecycleTests
{
    [Fact]
    public void Should_BeActive_And_RaiseCustomerCreated_WhenCreated()
    {
        CustomerId id = CustomerId.New();
        CustomerCode code = TestData.Code();

        var result = Customer.Create(id, TestData.Tenant, code, TestData.Name());

        result.IsSuccess.Should().BeTrue();
        Customer customer = result.Value;
        customer.Id.Should().Be(id);
        customer.TenantId.Should().Be(TestData.Tenant);
        customer.Status.Should().Be(CustomerStatus.Active);
        customer.DomainEvents.Should().ContainSingle();

        CustomerCreated created = customer.DomainEvents.OfType<CustomerCreated>().Single();
        created.CustomerId.Should().Be(id);
        created.CustomerCode.Should().Be(code);
        created.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Should_RaiseCustomerDeactivated_WhenDeactivated()
    {
        Customer customer = TestData.CreateCustomer();

        var result = customer.Deactivate();

        result.IsSuccess.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Deactivated);
        customer.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CustomerDeactivated>();
    }

    [Fact]
    public void Should_RaiseCustomerActivated_WhenReactivated()
    {
        Customer customer = TestData.CreateCustomer();
        customer.Deactivate();
        customer.ClearDomainEvents();

        var result = customer.Activate();

        result.IsSuccess.Should().BeTrue();
        customer.Status.Should().Be(CustomerStatus.Active);
        customer.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<CustomerActivated>();
    }

    [Fact]
    public void Should_Fail_WhenActivatingActiveCustomer()
    {
        Customer customer = TestData.CreateCustomer();

        var result = customer.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.CustomerAlreadyActive);
        customer.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_WhenDeactivatingDeactivatedCustomer()
    {
        Customer customer = TestData.CreateCustomer();
        customer.Deactivate();
        customer.ClearDomainEvents();

        var result = customer.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.CustomerAlreadyDeactivated);
        customer.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_ChangeName_WhenRequested()
    {
        Customer customer = TestData.CreateCustomer();
        CustomerName name = TestData.Name("New Name");

        var result = customer.ChangeName(name);

        result.IsSuccess.Should().BeTrue();
        customer.Name.Should().Be(name);
    }

    [Fact]
    public void Should_ChangeTaxIdentifier_WhenRequested()
    {
        Customer customer = TestData.CreateCustomer();
        TaxIdentifier taxIdentifier = TestData.Tax();

        customer.ChangeTaxIdentifier(taxIdentifier).IsSuccess.Should().BeTrue();
        customer.TaxIdentifier.Should().Be(taxIdentifier);

        customer.ChangeTaxIdentifier(null).IsSuccess.Should().BeTrue();
        customer.TaxIdentifier.Should().BeNull();
    }
}
