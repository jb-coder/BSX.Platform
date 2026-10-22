using BSX.Modules.Crm.Application.Abstractions;
using BSX.Modules.Crm.Application.Addresses.AddAddress;
using BSX.Modules.Crm.Application.Addresses.RemoveAddress;
using BSX.Modules.Crm.Application.Addresses.SetDefaultAddress;
using BSX.Modules.Crm.Application.Addresses.UpdateAddress;
using BSX.Modules.Crm.Application.Common;
using BSX.Modules.Crm.Application.Contacts.AddContact;
using BSX.Modules.Crm.Application.Contacts.RemoveContact;
using BSX.Modules.Crm.Application.Contacts.SetPrimaryContact;
using BSX.Modules.Crm.Application.Contacts.UpdateContact;
using BSX.Modules.Crm.Application.Errors;
using BSX.Modules.Crm.Application.UnitTests.Fakes;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Application.UnitTests.Customers;

public sealed class ContactAddressCommandHandlerTests
{
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task Should_AddContact_AndPersist()
    {
        Guid customerId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        _customers.Seed(customer);

        var handler = new AddContactCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(
            new AddContactCommand(customerId, "Jane Doe", "jane@example.com", "+1 555 0100", true));

        result.IsSuccess.Should().BeTrue();
        customer.Contacts.Should().ContainSingle();
        customer.Contacts.Single().IsPrimary.Should().BeTrue();
        _customers.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenContactHasNoReachabilityChannel()
    {
        Guid customerId = Guid.NewGuid();
        _customers.Seed(TestData.CreateCustomer(id: customerId));

        var handler = new AddContactCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new AddContactCommand(customerId, "Jane Doe", null, null, false));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CRM.Customer.ContactUnreachable");
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_UpdateContact()
    {
        Guid customerId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        customer.AddContact(ContactPersonId.From(contactId), TestData.Name("Jane"), TestData.Email("jane@example.com"))
            .IsSuccess.Should().BeTrue();
        _customers.Seed(customer);

        var handler = new UpdateContactCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(
            new UpdateContactCommand(customerId, contactId, "Jane Smith", "jane.smith@example.com", null));

        result.IsSuccess.Should().BeTrue();
        customer.Contacts.Single().Name.Value.Should().Be("Jane Smith");
        customer.Contacts.Single().Email!.Value.Should().Be("jane.smith@example.com");
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_RemoveContact()
    {
        Guid customerId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        customer.AddContact(ContactPersonId.From(contactId), TestData.Name("Jane"), TestData.Email())
            .IsSuccess.Should().BeTrue();
        _customers.Seed(customer);

        var handler = new RemoveContactCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new RemoveContactCommand(customerId, contactId));

        result.IsSuccess.Should().BeTrue();
        customer.Contacts.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_SetPrimaryContact()
    {
        Guid customerId = Guid.NewGuid();
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        customer.AddContact(ContactPersonId.From(firstId), TestData.Name("Jane"), TestData.Email("jane@example.com"))
            .IsSuccess.Should().BeTrue();
        customer.AddContact(ContactPersonId.From(secondId), TestData.Name("John"), TestData.Email("john@example.com"))
            .IsSuccess.Should().BeTrue();
        _customers.Seed(customer);

        var handler = new SetPrimaryContactCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new SetPrimaryContactCommand(customerId, secondId));

        result.IsSuccess.Should().BeTrue();
        customer.Contacts.Single(contact => contact.Id == ContactPersonId.From(secondId)).IsPrimary.Should().BeTrue();
        customer.Contacts.Single(contact => contact.Id == ContactPersonId.From(firstId)).IsPrimary.Should().BeFalse();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_AddAddress_AndPersist()
    {
        Guid customerId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        _customers.Seed(customer);

        var handler = new AddAddressCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(
            new AddAddressCommand(customerId, TestData.AddressInput(), CrmAddressType.Billing));

        result.IsSuccess.Should().BeTrue();
        customer.Addresses.Should().ContainSingle();
        customer.Addresses.Single().IsDefault.Should().BeTrue();
        _customers.LastRequestedTenant.Should().Be(TestData.TenantValue());
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenAddressIsInvalid()
    {
        Guid customerId = Guid.NewGuid();
        _customers.Seed(TestData.CreateCustomer(id: customerId));

        var handler = new AddAddressCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(
            new AddAddressCommand(customerId, TestData.AddressInput(city: " "), CrmAddressType.Billing));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CRM.Address.Invalid");
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_UpdateAddress()
    {
        Guid customerId = Guid.NewGuid();
        Guid addressId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        Address original = Address.Create("1 Main St", null, "Springfield", null, null, "US").Value;
        customer.AddAddress(CustomerAddressId.From(addressId), original, AddressType.Billing)
            .IsSuccess.Should().BeTrue();
        _customers.Seed(customer);

        var handler = new UpdateAddressCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(
            new UpdateAddressCommand(customerId, addressId, TestData.AddressInput("9 New St"), CrmAddressType.Shipping));

        result.IsSuccess.Should().BeTrue();
        customer.Addresses.Single().Address.Line1.Should().Be("9 New St");
        customer.Addresses.Single().Type.Should().Be(AddressType.Shipping);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_RemoveAddress()
    {
        Guid customerId = Guid.NewGuid();
        Guid addressId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        customer.AddAddress(
            CustomerAddressId.From(addressId),
            Address.Create("1 Main St", null, "Springfield", null, null, "US").Value,
            AddressType.Billing).IsSuccess.Should().BeTrue();
        _customers.Seed(customer);

        var handler = new RemoveAddressCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new RemoveAddressCommand(customerId, addressId));

        result.IsSuccess.Should().BeTrue();
        customer.Addresses.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_SetDefaultAddress()
    {
        Guid customerId = Guid.NewGuid();
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();
        Customer customer = TestData.CreateCustomer(id: customerId);
        customer.AddAddress(
            CustomerAddressId.From(firstId),
            Address.Create("1 Main St", null, "Springfield", null, null, "US").Value,
            AddressType.Billing).IsSuccess.Should().BeTrue();
        customer.AddAddress(
            CustomerAddressId.From(secondId),
            Address.Create("2 Oak St", null, "Springfield", null, null, "US").Value,
            AddressType.Shipping).IsSuccess.Should().BeTrue();
        _customers.Seed(customer);

        var handler = new SetDefaultAddressCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(new SetDefaultAddressCommand(customerId, secondId));

        result.IsSuccess.Should().BeTrue();
        customer.Addresses.Single(address => address.Id == CustomerAddressId.From(secondId)).IsDefault.Should().BeTrue();
        customer.Addresses.Single(address => address.Id == CustomerAddressId.From(firstId)).IsDefault.Should().BeFalse();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_WhenCustomerNotFound()
    {
        var handler = new AddContactCommandHandler(_customers, _unitOfWork, TenantResolver());
        var result = await handler.HandleAsync(
            new AddContactCommand(Guid.NewGuid(), "Jane", "jane@example.com", null, false));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmApplicationErrors.CustomerNotFound);
    }

    private CrmTenantResolver TenantResolver() => new(new FakeTenantContext(TestData.Tenant));
}
