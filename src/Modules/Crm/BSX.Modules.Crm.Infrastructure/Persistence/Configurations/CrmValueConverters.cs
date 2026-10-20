using BSX.Modules.Crm.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ValueObjects = BSX.Modules.Crm.Domain.ValueObjects;

namespace BSX.Modules.Crm.Infrastructure.Persistence.Configurations;

/// <summary>Value converters for CRM identifiers and value objects.</summary>
internal static class CrmValueConverters
{
    public static readonly ValueConverter<CustomerId, Guid> CustomerId =
        new(value => value.Value, value => ValueObjects.CustomerId.From(value));

    public static readonly ValueConverter<OrganizationId, Guid> OrganizationId =
        new(value => value.Value, value => ValueObjects.OrganizationId.From(value));

    public static readonly ValueConverter<ContactPersonId, Guid> ContactPersonId =
        new(value => value.Value, value => ValueObjects.ContactPersonId.From(value));

    public static readonly ValueConverter<CustomerAddressId, Guid> CustomerAddressId =
        new(value => value.Value, value => ValueObjects.CustomerAddressId.From(value));

    public static readonly ValueConverter<TenantId, Guid> TenantId =
        new(value => value.Value, value => ValueObjects.TenantId.From(value));

    public static readonly ValueConverter<CustomerCode, string> CustomerCode =
        new(value => value.Value, value => ValueObjects.CustomerCode.Create(value).Value);

    public static readonly ValueConverter<CustomerName, string> CustomerName =
        new(value => value.Value, value => ValueObjects.CustomerName.Create(value).Value);

    public static readonly ValueConverter<TaxIdentifier, string> TaxIdentifier =
        new(value => value.Value, value => ValueObjects.TaxIdentifier.Create(value).Value);

    public static readonly ValueConverter<Email, string> Email =
        new(value => value.Value, value => ValueObjects.Email.Create(value).Value);

    public static readonly ValueConverter<PhoneNumber, string> PhoneNumber =
        new(value => value.Value, value => ValueObjects.PhoneNumber.Create(value).Value);
}
