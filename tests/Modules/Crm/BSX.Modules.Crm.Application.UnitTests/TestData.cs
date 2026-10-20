using BSX.Modules.Crm.Domain.Customers;
using BSX.Modules.Crm.Domain.Organizations;
using BSX.Modules.Crm.Domain.ValueObjects;
using BSX.Modules.Crm.Application.Dtos;

namespace BSX.Modules.Crm.Application.UnitTests;

/// <summary>Shared builders for CRM application tests.</summary>
internal static class TestData
{
    internal static readonly Guid Tenant = Guid.NewGuid();

    internal static CustomerCode Code(string value = "CUST-001") => CustomerCode.Create(value).Value;

    internal static CustomerName Name(string value = "Acme Corporation") => CustomerName.Create(value).Value;

    internal static Email Email(string value = "contact@example.com")
        => BSX.Modules.Crm.Domain.ValueObjects.Email.Create(value).Value;

    internal static PhoneNumber Phone(string value = "+1 555 0100") => PhoneNumber.Create(value).Value;

    internal static TaxIdentifier Tax(string value = "TAX-123") => TaxIdentifier.Create(value).Value;

    internal static TenantId TenantValue(Guid? tenantId = null) => TenantId.From(tenantId ?? Tenant);

    internal static AddressInput AddressInput(
        string line1 = "1 Main St",
        string? line2 = null,
        string city = "Springfield",
        string? region = null,
        string? postalCode = null,
        string country = "US")
        => new(line1, line2, city, region, postalCode, country);

    internal static Customer CreateCustomer(
        Guid? id = null,
        string code = "CUST-001",
        string name = "Acme Corporation",
        Guid? tenantId = null)
    {
        Customer customer = Customer.Create(
            id is { } value ? CustomerId.From(value) : CustomerId.New(),
            TenantValue(tenantId),
            Code(code),
            Name(name)).Value;
        customer.ClearDomainEvents();
        return customer;
    }

    internal static Organization CreateOrganization(
        Guid? id = null,
        string name = "Holding Group",
        Guid? tenantId = null)
    {
        Organization organization = Organization.Create(
            id is { } value ? OrganizationId.From(value) : OrganizationId.New(),
            TenantValue(tenantId),
            Name(name)).Value;
        return organization;
    }
}
