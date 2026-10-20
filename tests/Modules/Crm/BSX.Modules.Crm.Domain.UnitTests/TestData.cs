using BSX.Modules.Crm.Domain.Contacts;
using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Domain.UnitTests;

/// <summary>Shared builders for CRM domain tests.</summary>
internal static class TestData
{
    internal static readonly TenantId Tenant = TenantId.New();

    internal static CustomerCode Code(string value = "CUST-001") => CustomerCode.Create(value).Value;

    internal static CustomerName Name(string value = "Acme Corporation") => CustomerName.Create(value).Value;

    internal static Email Email(string value = "contact@example.com")
        => BSX.Modules.Crm.Domain.ValueObjects.Email.Create(value).Value;

    internal static PhoneNumber Phone(string value = "+1 555 0100") => PhoneNumber.Create(value).Value;

    internal static TaxIdentifier Tax(string value = "TAX-123") => TaxIdentifier.Create(value).Value;

    internal static Address Address(
        string line1 = "1 Main St",
        string city = "Springfield",
        string country = "US")
        => BSX.Modules.Crm.Domain.ValueObjects.Address.Create(line1, null, city, null, null, country).Value;

    internal static Customer CreateCustomer(string code = "CUST-001", string name = "Acme Corporation")
    {
        Customer customer = Customer.Create(CustomerId.New(), Tenant, Code(code), Name(name)).Value;
        customer.ClearDomainEvents();
        return customer;
    }

    internal static Organization CreateOrganization(string name = "Holding Group")
        => Organization.Create(OrganizationId.New(), Tenant, Name(name)).Value;

    internal static Contact CreateContact(string name = "Jane Doe")
        => Contact.Create(ContactId.New(), Tenant, Name(name), Email(), Phone()).Value;
}
