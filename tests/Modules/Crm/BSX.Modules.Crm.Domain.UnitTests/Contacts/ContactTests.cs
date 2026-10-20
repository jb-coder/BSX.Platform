using BSX.Modules.Crm.Domain.Contacts;
using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.Errors;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests.Contacts;

public sealed class ContactTests
{
    [Fact]
    public void Should_CreateActiveContact_WhenValid()
    {
        ContactId id = ContactId.New();
        CustomerName name = TestData.Name("Jane Doe");
        Email email = TestData.Email();
        PhoneNumber phone = TestData.Phone();

        var result = Contact.Create(id, TestData.Tenant, name, email, phone);

        result.IsSuccess.Should().BeTrue();
        Contact contact = result.Value;
        contact.Id.Should().Be(id);
        contact.TenantId.Should().Be(TestData.Tenant);
        contact.Name.Should().Be(name);
        contact.Email.Should().Be(email);
        contact.PhoneNumber.Should().Be(phone);
        contact.CustomerId.Should().BeNull();
        contact.Status.Should().Be(ContactStatus.Active);
    }

    [Fact]
    public void Should_AssignToCustomer_WhenRequested()
    {
        Contact contact = TestData.CreateContact();
        CustomerId customerId = CustomerId.New();

        var result = contact.AssignToCustomer(customerId);

        result.IsSuccess.Should().BeTrue();
        contact.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void Should_Fail_WhenAssigningTheSameCustomer()
    {
        Contact contact = TestData.CreateContact();
        CustomerId customerId = CustomerId.New();
        contact.AssignToCustomer(customerId);

        var result = contact.AssignToCustomer(customerId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactAlreadyAssigned);
    }

    [Fact]
    public void Should_RemoveFromCustomer_WhenAssigned()
    {
        Contact contact = TestData.CreateContact();
        contact.AssignToCustomer(CustomerId.New());

        var result = contact.RemoveFromCustomer();

        result.IsSuccess.Should().BeTrue();
        contact.CustomerId.Should().BeNull();
    }

    [Fact]
    public void Should_Fail_WhenRemovingFromCustomer_ButNotAssigned()
    {
        Contact contact = TestData.CreateContact();

        var result = contact.RemoveFromCustomer();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactNotAssigned);
    }

    [Fact]
    public void Should_DeactivateAndReactivate_WhenRequested()
    {
        Contact contact = TestData.CreateContact();

        contact.Deactivate().IsSuccess.Should().BeTrue();
        contact.Status.Should().Be(ContactStatus.Inactive);

        contact.Activate().IsSuccess.Should().BeTrue();
        contact.Status.Should().Be(ContactStatus.Active);
    }

    [Fact]
    public void Should_Fail_WhenActivatingActiveContact()
    {
        Contact contact = TestData.CreateContact();

        var result = contact.Activate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactAlreadyActive);
    }

    [Fact]
    public void Should_Fail_WhenDeactivatingInactiveContact()
    {
        Contact contact = TestData.CreateContact();
        contact.Deactivate();

        var result = contact.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CrmErrors.ContactAlreadyInactive);
    }

    [Fact]
    public void Should_ChangeDetails_WhenRequested()
    {
        Contact contact = TestData.CreateContact();
        CustomerName name = TestData.Name("Jane Smith");
        Email email = TestData.Email("jane.smith@example.com");
        PhoneNumber phone = TestData.Phone("+34 600 123 456");

        contact.ChangeName(name).IsSuccess.Should().BeTrue();
        contact.ChangeEmail(email).IsSuccess.Should().BeTrue();
        contact.ChangePhoneNumber(phone).IsSuccess.Should().BeTrue();

        contact.Name.Should().Be(name);
        contact.Email.Should().Be(email);
        contact.PhoneNumber.Should().Be(phone);
    }
}
