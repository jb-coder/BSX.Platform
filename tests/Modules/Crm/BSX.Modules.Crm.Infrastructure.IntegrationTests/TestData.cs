using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Enums;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;
using ValueObjects = BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Infrastructure.IntegrationTests;

/// <summary>Shared builders for CRM infrastructure tests.</summary>
internal static class TestData
{
    internal static readonly Guid TenantA = Guid.NewGuid();

    internal static readonly Guid TenantB = Guid.NewGuid();

    internal static TenantId Tenant(Guid? tenantId = null) => TenantId.From(tenantId ?? TenantA);

    internal static CustomerCode Code(string value = "CUST-001") => CustomerCode.Create(value).Value;

    internal static CustomerName Name(string value = "Acme Corporation") => CustomerName.Create(value).Value;

    internal static Email Email(string value = "contact@example.com") => ValueObjects.Email.Create(value).Value;

    internal static PhoneNumber Phone(string value = "+1 555 0100") => PhoneNumber.Create(value).Value;

    internal static TaxIdentifier Tax(string value = "TAX-123") => TaxIdentifier.Create(value).Value;

    internal static Address Address(
        string line1 = "1 Main St",
        string city = "Springfield",
        string country = "US")
        => ValueObjects.Address.Create(line1, null, city, null, null, country).Value;

    internal static Customer CreateCustomer(
        Guid? id = null,
        Guid? tenantId = null,
        string code = "CUST-001",
        string name = "Acme Corporation")
    {
        Customer customer = Customer.Create(
            id is { } value ? CustomerId.From(value) : CustomerId.New(),
            Tenant(tenantId),
            Code(code),
            Name(name)).Value;
        customer.ClearDomainEvents();
        return customer;
    }

    internal static Customer AddAddress(
        Customer customer,
        string line1 = "1 Main St",
        AddressType type = AddressType.Billing,
        bool isDefault = true)
    {
        customer.AddAddress(CustomerAddressId.New(), Address(line1), type, isDefault)
            .IsSuccess.Should().BeTrue();
        return customer;
    }

    internal static Customer AddContact(
        Customer customer,
        string name = "Jane Doe",
        string email = "jane@example.com",
        bool isPrimary = true)
    {
        customer.AddContact(ContactPersonId.New(), Name(name), Email(email), Phone(), isPrimary)
            .IsSuccess.Should().BeTrue();
        return customer;
    }

    internal static Organization CreateOrganization(
        Guid? id = null,
        Guid? tenantId = null,
        string name = "Holding Group",
        string? taxIdentifier = "TAX-999",
        Address? address = null)
    {
        Organization organization = Organization.Create(
            id is { } value ? OrganizationId.From(value) : OrganizationId.New(),
            Tenant(tenantId),
            Name(name),
            taxIdentifier is null ? null : Tax(taxIdentifier),
            address).Value;
        return organization;
    }
}
