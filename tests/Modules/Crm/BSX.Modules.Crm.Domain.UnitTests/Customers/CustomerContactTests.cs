using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.DomainEvents;
using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.Customers;

public sealed class CustomerContactTests
{
    [Fact]
    public void Should_RaiseContactAdded_And_MakeFirstContactPrimary_WhenAdded()
    {
        Customer customer = TestData.CreateCustomer();
        ContactPersonId contactId = ContactPersonId.New();
        Email email = TestData.Email();

        var result = customer.AddContact(contactId, TestData.Name("Jane Doe"), email, TestData.Phone());

        result.IsSuccess.Should().BeTrue();
        customer.Contacts.Should().ContainSingle();
        customer.Contacts.Single().IsPrimary.Should().BeTrue();
        customer.Contacts.Single().Email.Should().Be(email);

        ContactAdded added = customer.DomainEvents.OfType<ContactAdded>().Single();
        added.CustomerId.Should().Be(customer.Id);
        added.ContactPersonId.Should().Be(contactId);
    }

    [Fact]
    public void Should_Fail_WhenContactEmailIsDuplicated()
    {
        Customer customer = TestData.CreateCustomer();
        Email email = TestData.Email();
        customer.AddContact(ContactPersonId.New(), TestData.Name("Jane"), email);

        var result = customer.AddContact(ContactPersonId.New(), TestData.Name("John"), email);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactEmailDuplicate);
        customer.Contacts.Should().ContainSingle();
    }

    [Fact]
    public void Should_KeepSinglePrimaryContact_WhenAddingAnotherPrimary()
    {
        Customer customer = TestData.CreateCustomer();
        customer.AddContact(ContactPersonId.New(), TestData.Name("Jane"), TestData.Email("jane@example.com"));
        ContactPersonId secondId = ContactPersonId.New();

        customer.AddContact(secondId, TestData.Name("John"), TestData.Email("john@example.com"), isPrimary: true);

        customer.Contacts.Single(contact => contact.IsPrimary).Id.Should().Be(secondId);
    }

    [Fact]
    public void Should_RaiseContactRemoved_And_PromoteRemainingContact_WhenPrimaryRemoved()
    {
        Customer customer = TestData.CreateCustomer();
        ContactPersonId firstId = ContactPersonId.New();
        ContactPersonId secondId = ContactPersonId.New();
        customer.AddContact(firstId, TestData.Name("Jane"), TestData.Email("jane@example.com"));
        customer.AddContact(secondId, TestData.Name("John"), TestData.Email("john@example.com"), isPrimary: true);
        customer.ClearDomainEvents();

        var result = customer.RemoveContact(secondId);

        result.IsSuccess.Should().BeTrue();
        customer.Contacts.Should().ContainSingle();
        customer.Contacts.Single().Id.Should().Be(firstId);
        customer.Contacts.Single().IsPrimary.Should().BeTrue();
        customer.DomainEvents.OfType<ContactRemoved>().Single().ContactPersonId.Should().Be(secondId);
    }

    [Fact]
    public void Should_Fail_WhenRemovingUnknownContact()
    {
        Customer customer = TestData.CreateCustomer();

        var result = customer.RemoveContact(ContactPersonId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactNotFound);
    }

    [Fact]
    public void Should_Fail_WhenAddingContactToDeactivatedCustomer()
    {
        Customer customer = TestData.CreateCustomer();
        customer.Deactivate();

        var result = customer.AddContact(ContactPersonId.New(), TestData.Name("Jane"), TestData.Email());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.CustomerNotActive);
    }

    [Fact]
    public void Should_EnforceContactLimit()
    {
        Customer customer = TestData.CreateCustomer();

        for (int index = 0; index < Customer.MaxContacts; index++)
        {
            customer.AddContact(
                ContactPersonId.New(),
                TestData.Name($"Contact {index}"),
                TestData.Email($"contact{index}@example.com")).IsSuccess.Should().BeTrue();
        }

        var result = customer.AddContact(
            ContactPersonId.New(),
            TestData.Name("Overflow"),
            TestData.Email("overflow@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactLimitReached);
    }

    [Fact]
    public void Should_SetPrimaryContact_WhenRequested()
    {
        Customer customer = TestData.CreateCustomer();
        ContactPersonId firstId = ContactPersonId.New();
        ContactPersonId secondId = ContactPersonId.New();
        customer.AddContact(firstId, TestData.Name("Jane"), TestData.Email("jane@example.com"));
        customer.AddContact(secondId, TestData.Name("John"), TestData.Email("john@example.com"));

        customer.SetPrimaryContact(secondId).IsSuccess.Should().BeTrue();

        customer.Contacts.Single(contact => contact.Id == secondId).IsPrimary.Should().BeTrue();
        customer.Contacts.Single(contact => contact.Id == firstId).IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_WhenSettingUnknownContactAsPrimary()
    {
        Customer customer = TestData.CreateCustomer();

        var result = customer.SetPrimaryContact(ContactPersonId.New());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactNotFound);
    }

    [Fact]
    public void Should_ChangeContactDetails_WhenValid()
    {
        Customer customer = TestData.CreateCustomer();
        ContactPersonId contactId = ContactPersonId.New();
        customer.AddContact(contactId, TestData.Name("Jane"), TestData.Email("jane@example.com"));
        CustomerName newName = TestData.Name("Jane Smith");
        Email newEmail = TestData.Email("jane.smith@example.com");

        var result = customer.ChangeContactDetails(contactId, newName, newEmail, TestData.Phone());

        result.IsSuccess.Should().BeTrue();
        ContactPerson contact = customer.Contacts.Single();
        contact.Name.Should().Be(newName);
        contact.Email.Should().Be(newEmail);
    }

    [Fact]
    public void Should_Fail_WhenChangingContactEmailToDuplicate()
    {
        Customer customer = TestData.CreateCustomer();
        ContactPersonId firstId = ContactPersonId.New();
        customer.AddContact(firstId, TestData.Name("Jane"), TestData.Email("jane@example.com"));
        customer.AddContact(ContactPersonId.New(), TestData.Name("John"), TestData.Email("john@example.com"));

        var result = customer.ChangeContactDetails(
            firstId,
            TestData.Name("Jane"),
            TestData.Email("john@example.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactEmailDuplicate);
    }

    [Fact]
    public void Should_Fail_WhenContactHasNoReachabilityChannel()
    {
        Customer customer = TestData.CreateCustomer();

        var result = customer.AddContact(ContactPersonId.New(), TestData.Name("Jane"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactUnreachable);
    }

    [Fact]
    public void Should_Fail_WhenChangeLeavesContactWithoutReachabilityChannel()
    {
        Customer customer = TestData.CreateCustomer();
        ContactPersonId contactId = ContactPersonId.New();
        customer.AddContact(contactId, TestData.Name("Jane"), TestData.Email("jane@example.com"));

        var result = customer.ChangeContactDetails(contactId, TestData.Name("Jane"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactUnreachable);
    }

    [Fact]
    public void Should_AllowAdministrativeChanges_WhenCustomerDeactivated()
    {
        Customer customer = TestData.CreateCustomer();
        ContactPersonId contactId = ContactPersonId.New();
        customer.AddContact(contactId, TestData.Name("Jane"), TestData.Email("jane@example.com"));
        customer.Deactivate();

        customer.ChangeName(TestData.Name("Renamed")).IsSuccess.Should().BeTrue();
        customer.AddAddress(CustomerAddressId.New(), TestData.Address(), AddressType.Billing).IsSuccess.Should().BeTrue();
        customer.ChangeContactDetails(contactId, TestData.Name("Jane Smith"), TestData.Email("jane.smith@example.com")).IsSuccess.Should().BeTrue();
        customer.RemoveContact(contactId).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_AllowSameContactEmail_AcrossCustomers()
    {
        Email email = TestData.Email("shared@example.com");
        Customer first = TestData.CreateCustomer("CUST-001", "First Customer");
        Customer second = TestData.CreateCustomer("CUST-002", "Second Customer");

        first.AddContact(ContactPersonId.New(), TestData.Name("Jane"), email).IsSuccess.Should().BeTrue();
        second.AddContact(ContactPersonId.New(), TestData.Name("Jane"), email).IsSuccess.Should().BeTrue();
    }
}
